using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Utility
{
    public static class QueueSizes
    {
        public const int Small = 5;
        public const int Medium = 10;
        public const int Large = 50;
    }

    public sealed class NotifyingQueue<T>
    {
        private readonly Queue<T> _queue;
        private readonly object _lockGuard = new object();
        private int _size;

        private bool _queueLocked;

        public bool IsLocked => _queueLocked;

        public event Action ValueEnqueued;
        public event Action QueueUnlocked;

        public int MaxSize
        {
            get { return _size; }
            set
            {
                lock (_lockGuard)
                {
                    if (value == _size)
                        return;
                    var oldSize = _size;
                    _size = value;
                    if (_queueLocked)
                    {
                        if (value > oldSize)
                        {
                            _queueLocked = false;
                            QueueUnlocked?.Invoke();
                        }
                    }
                    else
                    {
                        if (value < oldSize)
                        {
                            _queueLocked = true;
                        }
                    }
                }
            }
        }

        public NotifyingQueue(int maxSize = QueueSizes.Small)
        {
            _queue = new Queue<T>(maxSize);
            _queueLocked = false;
            _size = maxSize;
        }

        public bool TryEnqueue(T value)
        {
            lock (_lockGuard)
            {
                if(!_queueLocked)
                {
                    _queue.Enqueue(value);
                    ValueEnqueued?.Invoke();
                    if (_queue.Count >= _size)
                    {
                        _queueLocked = true;
                    }
                    return true;
                }
                return false;
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
                if(_queueLocked && _queue.Count < _size)
                {
                    _queueLocked = false;
                    QueueUnlocked?.Invoke();
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
