using Lidsvaldr.WorkflowComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    /// <summary>
    /// Represents an output entity for node entity.
    /// </summary>
    public class NodeOutput
    {
        /// <summary>
        /// Type of output value.
        /// </summary>
        private readonly Type _valueType;
        /// <summary>
        /// Value source set. Used for non-exclusive mode.
        /// </summary>
        private readonly SortedSet<OutputSource> _sources = new SortedSet<OutputSource>();
        /// <summary>
        /// Mutex.
        /// </summary>
        private readonly object _lockGuard = new object();
        /// <summary>
        /// Indicates whether node output can give value to only one consumer or many consumers.
        /// </summary>
        private bool _exclusiveModeEnabled;
        /// <summary>
        /// Value source. Used for exclusive mode.
        /// </summary>
        private OutputSource _globalSource;
        /// <summary>
        /// Max queue size for source(s) queues.
        /// </summary>
        private int _queueSize;

        /// <summary>
        /// Event to notify that the current output is ready for value taking. 
        /// </summary>
        public event Action OutputUnlocked;
        public event Action<Exception> ExceptionOccurred;

        internal void NotifyAboutException(Exception e)
        {
            ExceptionOccurred?.Invoke(e);
        }

        /// <summary>
        /// Indicates whether current output is locked.
        /// </summary>
        public bool IsLocked
        {
            get
            {
                return (_exclusiveModeEnabled && _globalSource.IsLocked) 
                    || (!_exclusiveModeEnabled && _sources.Any(x => x.IsLocked));
            }
        }

        /// <summary>
        /// Configures whether node should discard attempts to push value if output is locked or should wait until unlocking and try to push value again.
        /// </summary>
        public bool DiscardIfLocked { get; set; } = false;

        /// <summary>
        /// Type of output value.
        /// </summary>
        public Type ValueType => _valueType;

        /// <summary>
        /// Configures output source(s) queue size.
        /// </summary>
        public int QueueSize
        {
            get { return _queueSize; }
            set
            {
                lock (_lockGuard)
                {
                    if (value == _queueSize)
                        return;
                    _queueSize = value;
                    if (_globalSource != null)
                    {
                        _globalSource.Queue.MaxSize = value;
                    }
                    foreach (var source in _sources)
                    {
                        source.Queue.MaxSize = value;
                    }
                }
            }
        }

        /// <summary>
        /// Configures whether node output can give value to only one consumer or many consumers.
        /// </summary>
        public bool ExclusiveModeEnabled
        {
            get { return _exclusiveModeEnabled; }

            set
            {
                lock (_lockGuard)
                {
                    if (value == _exclusiveModeEnabled)
                        return;
                    _exclusiveModeEnabled = value;
                    InitQueues(_queueSize);
                }
            }
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="valueType">Type of output value.</param>
        /// <param name="exclusiveMode">Indicates whether node output can give value to only one consumer or many consumers.</param>
        /// <param name="size">Max size of source(s) queue.</param>
        public NodeOutput(Type valueType, bool exclusiveMode = true, int size = QueueSizes.IntLimited)
        {
            _exclusiveModeEnabled = exclusiveMode;
            _valueType = valueType;
            _queueSize = size;
            InitQueues(size);
        }

        /// <summary>
        /// Tries to push value to output source(s) queue and returns success status.
        /// </summary>
        /// <param name="value">Value to push.</param>
        /// <returns>True if value was successfully pushed or false otherwise.</returns>
        public bool TryPush(object value)
        {
            lock (_lockGuard)
            {
                if (IsLocked)
                {
                    return false;
                }
                if (_exclusiveModeEnabled)
                {
                    return _globalSource.Queue.TryEnqueue(value);
                }
                else
                {
                    foreach (var source in _sources)
                    {
                        source.Queue.TryEnqueue(value);
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// Tries to get output value source from current entity.
        /// </summary>
        /// <returns>New output source with the same queue as in current entity in exclusive mode or with new queue in non-exclusive mode.</returns>
        internal IValueSource TakeValueSource()
        {
            lock (_lockGuard)
            {
                NotifyingQueue<object> queue;
                if (_exclusiveModeEnabled)
                {
                    queue = _globalSource.Queue;
                }
                else
                {
                    queue = new NotifyingQueue<object>(_queueSize);
                }

                var source = new OutputSource(_valueType, queue);
                source.OutputUnlocked += () =>
                {
                    if (!IsLocked)
                    {
                        OutputUnlocked?.Invoke();
                    }
                };
                _sources.Add(source);
                return source;
            }
        }

        /// <summary>
        /// Initializes queues with specified size for all sources.
        /// </summary>
        /// <param name="size">Max queue size.</param>
        private void InitQueues(int size)
        {
            if (_exclusiveModeEnabled)
            {
                _globalSource = new OutputSource(_valueType, new NotifyingQueue<object>(size));
                foreach (var source in _sources)
                {
                    source.Queue = _globalSource.Queue;
                }
            }
            else
            {
                foreach (var source in _sources.Skip(1))
                {
                    source.Queue = new NotifyingQueue<object>(size);
                }
                _globalSource = null;
            }
        }
    }
}
