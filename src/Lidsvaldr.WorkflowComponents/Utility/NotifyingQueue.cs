using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Utility
{
    public sealed class NotifyingQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T> ();
        private readonly object _lockGuard = new object();

        public event Action ValueEnqueued;

        public void Enqueue(T value)
        {
            lock (_lockGuard)
            {
                _queue.Enqueue(value);
                ValueEnqueued?.Invoke();
            }
        }

        public bool TryDequeue(out T value)
        {
            lock (_lockGuard)
            {
                if (_queue.Count == 0)
                {
                    value = default(T);
                    return false;
                }
                value = _queue.Dequeue();
                return true;
            }
        }

        public bool Empty()
        {
            lock (_lockGuard)
            {
                return _queue.Count == 0;
            }
        }
    }
}
