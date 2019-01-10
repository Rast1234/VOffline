using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VkNet.Abstractions.Core;
using VkNet.Utils;

namespace VOffline.Services.Vk
{
    public class CancellableConstraint : IAwaitableConstraint
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly object _sync = new object();
        private readonly LimitedSizeStack<DateTime> _timeStamps;
        private int _count;
        private TimeSpan _timeSpan;
        private readonly CancellationToken token;

        public CancellableConstraint(int number, TimeSpan timeSpan, CancellationToken token)
        {
            if (number <= 0)
                throw new ArgumentException("count should be strictly positive", nameof(number));
            if (timeSpan.TotalMilliseconds <= 0.0)
                throw new ArgumentException("timeSpan should be strictly positive", nameof(timeSpan));
            this._count = number;
            this._timeSpan = timeSpan;
            this.token = token;
            this._timeStamps = new LimitedSizeStack<DateTime>(this._count);
        }

        public async Task<IDisposable> WaitForReadiness(CancellationToken cancellationToken)
        {

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken))
            {

                CancellableConstraint awaitableConstraint = this;
                await awaitableConstraint._semaphore.WaitAsync(cts.Token);
                int num = 0;
                DateTime now = DateTime.Now;
                DateTime dateTime = now - awaitableConstraint._timeSpan;
                LinkedListNode<DateTime> linkedListNode1 = awaitableConstraint._timeStamps.First;
                LinkedListNode<DateTime> linkedListNode2 = (LinkedListNode<DateTime>) null;
                while (linkedListNode1 != null && linkedListNode1.Value > dateTime)
                {
                    linkedListNode2 = linkedListNode1;
                    linkedListNode1 = linkedListNode1.Next;
                    ++num;
                }

                if (num < awaitableConstraint._count)
                    return (IDisposable) new DisposableAction(new Action(awaitableConstraint.OnEnded));
                TimeSpan delay = linkedListNode2.Value.Add(awaitableConstraint._timeSpan) - now;
                try
                {
                    await Task.Delay(delay, cts.Token);
                }
                catch (Exception)
                {
                    awaitableConstraint._semaphore.Release();
                    throw;
                }

                return (IDisposable) new DisposableAction(new Action(awaitableConstraint.OnEnded));
            }
        }

        /// <inheritdoc />
        public void SetRate(int number, TimeSpan timeSpan)
        {
            lock (this._sync)
            {
                this._count = number;
                this._timeSpan = timeSpan;
            }
        }

        private void OnEnded()
        {
            this._timeStamps.Push(DateTime.Now);
            this._semaphore.Release();
        }
    }
}