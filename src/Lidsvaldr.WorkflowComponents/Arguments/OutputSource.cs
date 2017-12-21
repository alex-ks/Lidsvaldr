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

        internal NotifyingQueue<object> Queue
        {
            set
            {
                _queue.ValueEnqueued -= ValueEnqueued;
                _queue = value;
                _queue.ValueEnqueued += ValueEnqueued;
            }
        }

        public bool IsExhausted => false;

        public bool IsValueReady => !_queue.Empty();

        public Type ValueType => _valueType;

        public event Action<IValueSource> ValueReady;

        public bool Pull(out object value)
        {
            return _queue.TryDequeue(out value);
        }

        private void ValueEnqueued()
        {
            ValueReady?.Invoke(this);
        }

        public OutputSource(Type valueType, NotifyingQueue<object> queue)
        {
            Queue = queue;
            _valueType = valueType;
        }
    }
}
