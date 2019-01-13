using System.Collections.Generic;

namespace VOffline.Services.VkNetHacks
{
    public class LimitedSizeStack<T> : LinkedList<T>
    {
        private readonly int _maxSize;

        public LimitedSizeStack(int maxSize)
        {
            this._maxSize = maxSize;
        }

        public void Push(T item)
        {
            this.AddFirst(item);
            if (this.Count <= this._maxSize)
                return;
            this.RemoveLast();
        }
    }
}