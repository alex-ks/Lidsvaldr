using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Utility
{
    /// <summary>
    /// Represents set of constants for configuring NotifyingQueue size.
    /// </summary>
    public static class QueueSizes
    {
        public const int Small = 5;
        public const int Medium = 10;
        public const int Large = 50;
        public const int IntLimited = int.MaxValue;
    }

    /// <summary>
    /// Represents queue with  fixed max size and ability to notify of state changing.
    /// </summary>
    /// <typeparam name="T">Type of queue element.</typeparam>
    public sealed class NotifyingQueue<T>
    {
        /// <summary>
        /// Queue of elements.
        /// </summary>
        private readonly Queue<T> _queue;
        /// <summary>
        /// Mutex.
        /// </summary>
        private readonly object _lockGuard = new object();
        /// <summary>
        /// Max queue size.
        /// </summary>
        private int _size;
        /// <summary>
        /// Used to specify when the queue reaches its maximum size and is blocked.
        /// </summary>
        private bool _queueLocked;

        /// <summary>
        /// Indicates whether queue is locked on not.
        /// </summary>
        public bool IsLocked => _queueLocked;

        /// <summary>
        /// Event to notify of the replenishment of the queue.
        /// </summary>
        public event Action ValueEnqueued;
        /// <summary>
        /// Event to notify that the queue is ready for replenishment.
        /// </summary>
        public event Action QueueUnlocked;

        /// <summary>
        /// Max queue size. Allows to configure max size.
        /// </summary>
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

        /// <summary>
        /// Class constructor. Sets necessary fields and initialize queue.
        /// </summary>
        /// <param name="maxSize">Max queue size.</param>
        public NotifyingQueue(int maxSize = QueueSizes.IntLimited)
        {
            if (maxSize != QueueSizes.IntLimited)
            {
                _queue = new Queue<T>(maxSize);
            }
            else
            {
                _queue = new Queue<T>();
            }
            _queueLocked = false;
            _size = maxSize;
        }

        /// <summary>
        /// Tries to enqueue new value and returns success status. Locks the queue after adding a new element if the queue reaches its maximum size.
        /// </summary>
        /// <param name="value">Value to enqueue.</param>
        /// <returns>True if value was successfully enqueue or false otherwise.</returns>
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

        /// <summary>
        /// Tries to dequeue value and returns success status. Unlocks the queue after element dequeue if the queue size became less than maximum size.
        /// </summary>
        /// <param name="value">Output parameter for dequeued value.</param>
        /// <returns>True if value was successfully dequeued or false otherwise.</returns>
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

        /// <summary>
        /// Returns whether queue is empty. 
        /// </summary>
        /// <returns>True if queue is empty or false otherwise.</returns>
        public bool Empty()
        {
            lock (_lockGuard)
            {
                return _queue.Count == 0;
            }
        }
    }
}
