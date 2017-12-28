using Lidsvaldr.WorkflowComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    public class NodeOutput
    {
        #region private fields
        private readonly Type _valueType;
        private readonly SortedSet<OutputSource> _sources = new SortedSet<OutputSource>();
        private readonly object _lockGuard = new object();
        private bool _exclusiveModeEnabled;
        private OutputSource _globalSource;
        private readonly QueueSizeEnum _queueSize;
        #endregion private fields

        #region public fields
        public bool IsLocked { get { return (_globalSource != null && _globalSource.IsLocked) || (_sources != null && _sources.Any(x => x.IsLocked)); } }
        #endregion public fields

        #region public methods
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

        public NodeOutput(Type valueType, bool exclusiveMode = true, QueueSizeEnum size = QueueSizeEnum.Small)
        {
            _exclusiveModeEnabled = exclusiveMode;
            _valueType = valueType;
            _queueSize = size;
            InitQueues(size);
        }

        public void Push(object value)
        {
            lock (_lockGuard)
            {
                if (_exclusiveModeEnabled)
                {
                    _globalSource.Queue.Enqueue(value);
                }
                else
                {
                    foreach (var source in _sources)
                    {
                        source.Queue.Enqueue(value);
                    }
                }
            }
        }
        #endregion public methods

        #region internal methods
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
                _sources.Add(source);
                return source;
            }
        }
        #endregion internal methods

        #region private methods
        private void InitQueues(QueueSizeEnum size)
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
        #endregion private methods
    }
}
