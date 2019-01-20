using System.IO;

namespace VOffline.Models.Storage
{
    public class Nested<T>
    {
        public Nested(T data, DirectoryInfo parentDir, string name)
        {
            Data = data;
            ParentDir = parentDir;
            Name = name;
        }

        public T Data { get; }
        public DirectoryInfo ParentDir { get; }
        public string Name { get; }
    }
}