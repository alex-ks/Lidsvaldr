using Lidsvaldr.WorkflowComponents.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    internal sealed class OutputSource : IValueSource
    {
        private readonly Type _valueType;
        private NotifyingQueue<object> _queue;

        public event Action OutputUnlocked;

        internal NotifyingQueue<object> Queue
        {
            get
            {
                return _queue;
            }

            set
            {
                if (_queue != null)
                {
                    _queue.ValueEnqueued -= ValueEnqueued;
                    _queue.QueueUnlocked -= QueueUnlocked;
                }
                _queue = value;
                _queue.ValueEnqueued += ValueEnqueued;
                _queue.QueueUnlocked += QueueUnlocked;
            }
        }

        public bool IsExhausted => false;

        public bool IsValueReady => !_queue.Empty();

        public Type ValueType => _valueType;

        public bool IsLocked => _queue.IsLocked;

        public event Action<IValueSource> ValueReady;

        public OutputSource(Type valueType, NotifyingQueue<object> queue)
        {
            Queue = queue;
            _valueType = valueType;
        }

        public bool Pull(out object value)
        {
            return _queue.TryDequeue(out value);
        }

        private void ValueEnqueued()
        {
            ValueReady?.Invoke(this);
        }

        private void QueueUnlocked()
        {
            OutputUnlocked?.Invoke();
        }
    }
}
