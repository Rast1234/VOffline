using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace VOffline.Services.Storage
{
    public class FilesystemTools
    {
        public DirectoryInfo MkDir(string path)
        {
            var dir = new DirectoryInfo(path);
            dir.Create();
            dir.Refresh();
            return dir;
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
                    case CreateMode.MergeWithExisting:
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
                    case CreateMode.MergeWithExisting:
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
            var completedName = $".{validName}.done.voffline";
            var file = new FileInfo(CombineCutPath(parent, validName));
            var completedFile = new FileInfo(CombineCutPath(parent, completedName));
            if (completedFile.Exists)
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
            await File.WriteAllTextAsync(completedFile.FullName, $"{DateTime.Now:O}", token);
            completedFile.Attributes |= FileAttributes.Hidden;
        }

        public async Task WriteFileWithCompletionMark(DirectoryInfo parent, string desiredName, Func<Task<byte[]>> contentTaskFunc, CancellationToken token, ILog log)
        {
            var validName = MakeValidName(desiredName);
            var completedName = $".{validName}.done.voffline";
            var file = new FileInfo(CombineCutPath(parent, validName));
            var completedFile = new FileInfo(CombineCutPath(parent, completedName));
            if (completedFile.Exists)
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
            await File.WriteAllTextAsync(completedFile.FullName, $"{DateTime.Now:O}", token);
            completedFile.Attributes |= FileAttributes.Hidden;
        }

        public int Wipe(DirectoryInfo dir)
        {
            lock (LockObject)
            {
                var count = 0;
                foreach (var f in dir.GetFiles())
                {
                    f.Delete();
                    count++;
                }
                foreach (var d in dir.GetDirectories())
                {
                    d.Delete(true);
                    count++;
                }

                return count;
            }
        }

        public void MarkAsCompleted(DirectoryInfo dir)
        {
            lock (LockObject)
            {
                var file = CreateFile(dir, CompletedFilename, CreateMode.ThrowIfExists);
                File.WriteAllText(file.FullName, $"{DateTime.Now:O}");
                file.Attributes |= FileAttributes.Hidden;
            }
        }

        public bool IsCompleted(DirectoryInfo dir)
        {
            var file = new FileInfo(CombineCutPath(dir, CompletedFilename));
            return file.Exists;
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
            var bakName = name;
            while (name.Length > 1)
            {
                try
                {
                    var testName = name + " (NNN)";  // extra filler for possible " (NNN)"
                    var testPath = Path.Combine(parentDir.FullName, testName);
                    Path.GetFullPath(testPath);  // test if throws
                    return Path.Combine(parentDir.FullName, name);
                }
                catch (PathTooLongException)
                {
                    name = name.Substring(0, Math.Max(1, name.Length - 1));
                }
            }
            throw new PathTooLongException($"Tried to shorten [{bakName}] to [{name}] but path [{parentDir.FullName}] is still too long");
        }

        private static string MakeValidName(string value) => string
            .Join("_", value.Split(AllBadChars))
            .Trim();

        private static readonly char[] AllBadChars =
            Path.GetInvalidFileNameChars()
                .Concat(Path.GetInvalidPathChars())
                .Concat(new[] { '\\', '/', ':' })
                .Distinct()
                .ToArray();

        private static readonly object LockObject = new object();

        private static readonly string CompletedFilename = MakeValidName(".done.voffline");
    }
}