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

    public abstract class Category
    {
        protected Category(long ownerId)
        {
            OwnerId = ownerId;
        }

        public long OwnerId { get; }
    }

    public class OrderedAttachment<T>
    {
        public OrderedAttachment(T data, int number, DirectoryInfo workingDir)
        {
            Data = data;
            Number = number;
            WorkingDir = workingDir;
        }

        public T Data { get; }
        public int Number { get; }
        public DirectoryInfo WorkingDir { get; }
    }
}