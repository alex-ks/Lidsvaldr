using Lidsvaldr.WorkflowComponents.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    /// <summary>
    /// Output source entity.
    /// </summary>
    internal sealed class OutputSource : IValueSource
    {
        /// <summary>
        /// Type of source value.
        /// </summary>
        private readonly Type _valueType;
        /// <summary>
        /// Queue of output values.
        /// </summary>
        private NotifyingQueue<object> _queue;

        /// <summary>
        /// Event to notify that the queue is ready for replenishment. 
        /// </summary>
        public event Action OutputUnlocked;

        /// <summary>
        /// Queue of output values. Configures event handlers for queue.
        /// </summary>
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

        /// <summary>
        /// Indicates whether source value is exhaused.
        /// </summary>
        public bool IsExhausted => false;

        /// <summary>
        /// Indicates whether source value is ready to be pulled.
        /// </summary>
        public bool IsValueReady => !_queue.Empty();

        /// <summary>
        /// Type of source value.
        /// </summary>
        public Type ValueType => _valueType;

        /// <summary>
        /// Indicates whether source is locked on not.
        /// </summary>
        public bool IsLocked => _queue.IsLocked;

        /// <summary>
        /// Event to notify the value readiness.
        /// </summary>
        public event Action<IValueSource> ValueReady;

        public OutputSource(Type valueType, NotifyingQueue<object> queue)
        {
            Queue = queue;
            _valueType = valueType;
        }

        /// <summary>
        /// Tries to get value and return success status. 
        /// </summary>
        /// <param name="value">Output parameter for pulled value.</param>
        /// <returns>True if value was successfully extracted or false otherwise.</returns>
        public bool Pull(out object value)
        {
            return _queue.TryDequeue(out value);
        }

        /// <summary>
        /// Event handler method for ValueEnqueued event fired by inner queue.
        /// </summary>
        private void ValueEnqueued()
        {
            ValueReady?.Invoke(this);
        }

        /// <summary>
        /// Event handler method for QueueUnlocked event  fired by inner queue.
        /// </summary>
        private void QueueUnlocked()
        {
            OutputUnlocked?.Invoke();
        }
    }
}
