using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Utility
{
    public sealed class NotifyingQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T> ();
        private readonly object _lockGuard = new object();
        private readonly QueueSizeEnum _size;

        private bool _queueLocked;

        public event Action ValueEnqueued;
        public event Action QueueLocked;
        public event Action QueueUnloked;

        public NotifyingQueue(QueueSizeEnum size = QueueSizeEnum.Small) {
            _queueLocked = false;
            _size = size;
        }

        public void Enqueue(T value)
        {
            lock (_lockGuard)
            {
                if(!_queueLocked)
                {
                    _queue.Enqueue(value);
                    ValueEnqueued?.Invoke();
                    if (_queue.Count == (int)_size)
                    {
                        _queueLocked = true;
                        QueueLocked?.Invoke();
                    }
                }
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
                if(_queueLocked && _queue.Count < (int)_size)
                {
                    _queueLocked = false;
                    QueueUnloked?.Invoke();
                }
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
