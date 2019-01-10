using System;
using System.IO;
using System.Linq;

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

        public DirectoryInfo CreateSubdir(DirectoryInfo parent, string desiredName, bool resolveCollisions)
        {
            if (!parent.Exists)
            {
                throw new DirectoryNotFoundException($"Parent {parent.FullName} does not exist, can not create dir {desiredName}");
            }

            var validName = MakeValidName(desiredName);
            lock (LockObject)
            {
                var newDir = resolveCollisions
                    ? GetUniqueDirectory(parent, validName)
                    : new DirectoryInfo(Path.Combine(parent.FullName, validName));
                if (newDir.Exists)
                {
                    throw new InvalidOperationException($"Dir already exists: {newDir.FullName}");
                }
                newDir.Create();
                newDir.Refresh();
                return newDir;
            }
        }

        public FileInfo CreateUniqueFile(DirectoryInfo parent, string desiredName)
        {
            if (!parent.Exists)
            {
                throw new DirectoryNotFoundException($"Parent {parent.FullName} does not exist, can not create file {desiredName}");
            }

            var validName = MakeValidName(desiredName);
            lock (LockObject)
            {
                var newFile = GetUniqueFile(parent, validName);
                if (newFile.Exists)
                {
                    throw new InvalidOperationException($"File already exists: {newFile.FullName}");
                }
                newFile.Create().Close();
                newFile.Refresh();
                return newFile;
            }
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
            var fileInfo = new FileInfo(Path.Combine(parent.FullName, validName));
            for (var i = 1;; i++)
            {
                if (!fileInfo.Exists)
                {
                    return fileInfo;
                }

                fileInfo = new FileInfo(Path.Combine(parent.FullName, $"{name} ({i}){extension}"));
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
            var directoryInfo = new DirectoryInfo(Path.Combine(parent.FullName, validName));
            for (var i = 1;; i++)
            {
                if (!directoryInfo.Exists)
                {
                    return directoryInfo;
                }

                directoryInfo = new DirectoryInfo(Path.Combine(parent.FullName, $"{validName} ({i})"));
            }
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
    }
}