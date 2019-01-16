using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using VOffline.Models;

namespace VOffline.Services.Storage
{
    public class FilesystemTools
    {
        private readonly FileInfo cacheFile;

        private readonly ConcurrentDictionary<string, DateTime> cache;

        public DirectoryInfo RootDir { get; }

        public FilesystemTools(IOptionsSnapshot<Settings> settings)
        {
            RootDir = MkDir(settings.Value.OutputPath);
            cacheFile = new FileInfo(CombineCutPath(RootDir, CacheFilename));
            cache = new ConcurrentDictionary<string, DateTime>();
        }

        public void LoadCache(ILog log)
        {
            lock (LockObject)
            {
                var lines = File.ReadAllLines(cacheFile.FullName);
                var separator = new[] {' '};
                foreach (var line in lines)
                {
                    var items = line.Split(separator, 2);
                    var datetime = DateTime.ParseExact(items[0], "O", CultureInfo.InvariantCulture);
                    var path = items[1];
                    cache[path] = datetime;
                }
            }

            log.Info($"Cache loaded: {cache.Count} items, {cacheFile.FullName}");
        }

        public void SaveCache(ILog log)
        {
            lock (LockObject)
            {
                var data = cache
                    .OrderBy(kv => kv.Value)
                    .Select(kv => $"{kv.Value:O} {kv.Key}");
                File.WriteAllLines(cacheFile.FullName, data);
            }

            log.Info($"Cache saved: {cache.Count} items, {cacheFile.FullName}");
        }

        public DirectoryInfo CreateSubdir(DirectoryInfo parent, string desiredName, CreateMode mode)
        {
            if (!parent.Exists)
            {
                throw new DirectoryNotFoundException($"Parent {parent.FullName} does not exist, can not create dir {desiredName}");
            }

            var validName = MakeValidName(desiredName);
            lock (LockObject)
            {
                DirectoryInfo directory;
                switch (mode)
                {
                    case CreateMode.AutoRenameCollisions:
                        directory = GetUniqueDirectory(parent, validName);
                        break;
                    case CreateMode.ThrowIfExists:
                        directory = new DirectoryInfo(CombineCutPath(parent, validName));
                        if (directory.Exists)
                        {
                            throw new InvalidOperationException($"Dir already exists: {directory.FullName}");
                        }

                        break;
                    case CreateMode.OverwriteExisting:
                        directory = new DirectoryInfo(CombineCutPath(parent, validName));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                }

                directory.Create();
                directory.Refresh();
                return directory;
            }
        }

        public FileInfo CreateFile(DirectoryInfo parent, string desiredName, CreateMode mode)
        {
            if (!parent.Exists)
            {
                throw new DirectoryNotFoundException($"Parent {parent.FullName} does not exist, can not create file {desiredName}");
            }

            var validName = MakeValidName(desiredName);
            lock (LockObject)
            {
                FileInfo file;
                switch (mode)
                {
                    case CreateMode.AutoRenameCollisions:
                        file = GetUniqueFile(parent, validName);
                        break;
                    case CreateMode.ThrowIfExists:
                        file = new FileInfo(CombineCutPath(parent, validName));
                        if (file.Exists)
                        {
                            throw new InvalidOperationException($"File already exists: {file.FullName}");
                        }

                        break;
                    case CreateMode.OverwriteExisting:
                        file = new FileInfo(CombineCutPath(parent, validName));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                }

                file.Create().Close();
                file.Refresh();
                return file;
            }
        }

        public async Task WriteFileWithCompletionMark(DirectoryInfo parent, string desiredName, Func<Task<string>> contentTaskFunc, CancellationToken token, ILog log)
        {
            var validName = MakeValidName(desiredName);
            var file = new FileInfo(CombineCutPath(parent, validName));
            if (cache.ContainsKey(file.FullName))
            {
                log.Debug($"Skipping [{file.FullName}] because marked as competed");
                return;
            }

            var content = await contentTaskFunc();
            if (string.IsNullOrEmpty(content))
            {
                log.Warn($"Content for [{file.FullName}] is empty");
            }
            else
            {
                await File.WriteAllTextAsync(file.FullName, content, token);
                log.Info($"Saved [{desiredName}] as [{file.FullName}] with [{content.Length}] chars");
            }
            cache[file.FullName] = DateTime.Now;
        }

        public async Task WriteFileWithCompletionMark(DirectoryInfo parent, string desiredName, Func<Task<byte[]>> contentTaskFunc, CancellationToken token, ILog log)
        {
            var validName = MakeValidName(desiredName);
            var file = new FileInfo(CombineCutPath(parent, validName));
            if (cache.ContainsKey(file.FullName))
            {
                log.Debug($"Skipping [{file.FullName}] because marked as competed");
                return;
            }

            var content = await contentTaskFunc();
            if (content == null || content.Length == 0)
            {
                log.Warn($"Content for [{file.FullName}] is empty");
            }
            else
            {
                await File.WriteAllBytesAsync(file.FullName, content, token);
                log.Info($"Saved [{desiredName}] as [{file.FullName}] with [{content.Length}] bytes");
            }

            cache[file.FullName] = DateTime.Now;
        }

        private DirectoryInfo MkDir(string path)
        {
            var dir = new DirectoryInfo(path);
            dir.Create();
            dir.Refresh();
            return dir;
        }

        /// <summary>
        /// Appends number to name in case of collision. Has no side effects. Should be used under lock.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="validName"></param>
        /// <returns></returns>
        private static FileInfo GetUniqueFile(DirectoryInfo parent, string validName)
        {
            var name = Path.GetFileNameWithoutExtension(validName);
            var extension = Path.GetExtension(validName);
            var fileInfo = new FileInfo(CombineCutPath(parent, validName));
            for (var i = 1;; i++)
            {
                if (!fileInfo.Exists)
                {
                    return fileInfo;
                }

                fileInfo = new FileInfo(CombineCutPath(parent, $"{name} ({i}){extension}"));
            }
        }

        /// <summary>
        /// Appends number to name in case of collision. Has no side effects. Should be used under lock. 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="validName"></param>
        /// <returns></returns>
        private static DirectoryInfo GetUniqueDirectory(DirectoryInfo parent, string validName)
        {
            var directoryInfo = new DirectoryInfo(CombineCutPath(parent, validName));
            for (var i = 1;; i++)
            {
                if (!directoryInfo.Exists)
                {
                    return directoryInfo;
                }

                directoryInfo = new DirectoryInfo(CombineCutPath(parent, $"{validName} ({i})"));
            }
        }

        private static string CombineCutPath(DirectoryInfo parentDir, string name)
        {
            var namePart = Path.GetFileNameWithoutExtension(name);
            var extensionPart = Path.GetExtension(name);
            while (namePart.Length > 1)
            {
                var testName = $"{namePart} (NNN){extensionPart}"; // extra filler for possible " (NNN)"
                var testPath = Path.Combine(parentDir.FullName, testName);
                if (testPath.Length < 248)
                {
                    return Path.Combine(parentDir.FullName, $"{namePart}{extensionPart}");
                }

                //Path.GetFullPath(testPath);  // should throw on long paths but does not work on net core?
                namePart = namePart.Substring(0, Math.Max(1, namePart.Length - 1));
            }

            throw new PathTooLongException($"Tried to shorten [{name}] to [{namePart}{extensionPart}] but path [{parentDir.FullName}] is still too long");
        }

        private static string MakeValidName(string value) => string
            .Join("_", value.Split(AllBadChars))
            .Trim();

        private static readonly char[] AllBadChars =
            Path.GetInvalidFileNameChars()
                .Concat(Path.GetInvalidPathChars())
                .Concat(new[] {'\\', '/', ':'})
                .Distinct()
                .ToArray();

        private static readonly object LockObject = new object();

        private static readonly string CacheFilename = MakeValidName(".cache.voffline");
    }
}