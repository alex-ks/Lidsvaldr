using Lidsvaldr.WorkflowComponents.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    internal sealed class OutputSource : IValueSource
    {
        #region private fields
        private readonly Type _valueType;
        private NotifyingQueue<object> _queue;
        #endregion private fields

        #region internal fields
        internal NotifyingQueue<object> Queue
        {
            get
            {
                return _queue;
            }

            set
            {
                _queue.ValueEnqueued -= ValueEnqueued;
                _queue = value;
                _queue.ValueEnqueued += ValueEnqueued;
            }
        }
        #endregion internal fields

        #region public fields
        public bool IsExhausted => false;

        public bool IsValueReady => !_queue.Empty();

        public Type ValueType => _valueType;

        public event Action<IValueSource> ValueReady;
        #endregion public fields

        #region public methods
        public bool Pull(out object value)
        {
            return _queue.TryDequeue(out value);
        }

        public OutputSource(Type valueType, NotifyingQueue<object> queue)
        {
            Queue = queue;
            _valueType = valueType;
        }
        #endregion public methods

        #region private methods
        private void ValueEnqueued()
        {
            ValueReady?.Invoke(this);
        }
        #endregion private methods
    }
}
