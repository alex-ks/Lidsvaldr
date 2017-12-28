using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Utility
{
    public sealed class NotifyingQueue<T>
    {
        #region private fields
        private readonly Queue<T> _queue = new Queue<T> ();
        private readonly object _lockGuard = new object();
        private QueueSizeEnum _size;

        private bool _queueLocked;
        #endregion private fields

        #region public fields
        public event Action ValueEnqueued;
        public event Action QueueLocked;
        public event Action QueueUnloked;

        public QueueSizeEnum Size
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
                            QueueUnloked?.Invoke();
                        }
                    }
                    else
                    {
                        if (value < oldSize)
                        {
                            _queueLocked = true;
                            QueueLocked?.Invoke();
                        }
                    }
                }
            }
        }
        #endregion public fields

        #region public methods
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
        #endregion public methods
    }
}
