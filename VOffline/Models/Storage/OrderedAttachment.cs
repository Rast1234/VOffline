using System.IO;

namespace VOffline.Models.Storage
{
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