using System;
using System.Collections.Generic;

namespace VOffline.Services.Queues
{
    public class QueueItem<TData>
    {
        public QueueItem(TData data)
        {
            Data = data;
            Errors = new Lazy<List<Exception>>();
        }

        public TData Data { get; }
        public Lazy<List<Exception>> Errors { get; }
    }
}