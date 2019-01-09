using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VkNet.Model.RequestParams;

namespace VOffline.Services
{
    public class Storage
    {
        private readonly DirectoryInfo rootDir;

        public string FullPath => rootDir.FullName;

        public Storage(string rootName)
        {
            rootDir = new DirectoryInfo(FilterFilename(rootName));
            lock (LockObject)
            {
                if (rootDir.Exists)
                {
                    throw new InvalidOperationException($"Storage directory [{rootDir.Name}] already exists");
                }
                rootDir.Create();
            }
        }

        private Storage(string rootName, bool useExisting)
        {
            rootDir = new DirectoryInfo(rootName);
            lock (LockObject)
            {
                if (!useExisting && rootDir.Exists)
                {
                    throw new InvalidOperationException($"Storage directory [{rootDir.Name}] already exists");
                }
                rootDir.Create();
            }
        }

        public Storage Descend(string subName, bool makeUniqueName)
        {
            var filteredName = FilterFilename(subName);
            if (Path.IsPathRooted(filteredName))
            {
                throw new InvalidOperationException($"Expected relative path, got absolute: [{filteredName}]");
            }

            // TODO: looks like cancer. also probable race conditions?
            if (makeUniqueName)
            {
                var uniqueName = GetUniqueDirectory(filteredName);
                return new Storage(Path.Combine(rootDir.FullName, uniqueName.Name), true);
            }
            return new Storage(Path.Combine(rootDir.FullName, filteredName), false);
        }

        /// <summary>
        /// Creates unique file
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FileInfo GetFile(string name)
        {
            var filename = FilterFilename(name);
            var file = GetUniqueFile(filename);
            return file;
        }

        private FileInfo GetUniqueFile(string filteredName)
        {
            var name = Path.GetFileNameWithoutExtension(filteredName);
            var extension = Path.GetExtension(filteredName);
            var fileInfo = new FileInfo(Path.Combine(rootDir.FullName, filteredName));
            for (var i = 1;; i++)
            {
                lock (LockObject)
                {
                    if (!fileInfo.Exists)
                    {
                        fileInfo.Create().Close();
                        return fileInfo;
                    }
                }
                fileInfo = new FileInfo(Path.Combine(rootDir.FullName, $"{name} ({i}){extension}"));
            }
        }

        private DirectoryInfo GetUniqueDirectory(string filteredName)
        {
            var directoryInfo = new DirectoryInfo(Path.Combine(rootDir.FullName, filteredName));
            for (var i = 1; ; i++)
            {
                lock (LockObject)
                {
                    if (!directoryInfo.Exists)
                    {
                        directoryInfo.Create();
                        return directoryInfo;
                    }
                }
                directoryInfo = new DirectoryInfo(Path.Combine(rootDir.FullName, $"{filteredName} ({i})"));
            }
        }

        private static string FilterFilename(string value) => string.Join("_", value.Split(AllBadChars)).Trim();

        private static readonly char[] AllBadChars =
            Path.GetInvalidFileNameChars()
                .Concat(Path.GetInvalidPathChars())
                .Concat(new[] {'\\', '/', ':'})
                .Distinct()
                .ToArray();

        private static readonly object LockObject = new object();
    }
}