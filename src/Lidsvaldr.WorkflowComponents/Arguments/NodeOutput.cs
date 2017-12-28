using Lidsvaldr.WorkflowComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    public class NodeOutput
    {
        private readonly Type _valueType;
        private readonly SortedSet<OutputSource> _sources = new SortedSet<OutputSource>();
        private readonly object _lockGuard = new object();
        private bool _exclusiveModeEnabled;
        private OutputSource _globalSource;
        private int _queueSize;

        public event Action OutputUnlocked;

        public bool IsLocked
        {
            get
            {
                return (_exclusiveModeEnabled && _globalSource.IsLocked) 
                    || (!_exclusiveModeEnabled && _sources.Any(x => x.IsLocked));
            }
        }

        public bool DiscardIfLocked { get; set; } = false;

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

        public NodeOutput(Type valueType, bool exclusiveMode = true, int size = QueueSizes.IntLimited)
        {
            _exclusiveModeEnabled = exclusiveMode;
            _valueType = valueType;
            _queueSize = size;
            InitQueues(size);
        }

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
