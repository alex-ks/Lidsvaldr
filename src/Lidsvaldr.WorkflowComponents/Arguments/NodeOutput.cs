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
        private NotifyingQueue<object> _globalQueue;

        private void InitQueues()
        {
            if (_exclusiveModeEnabled)
            {
                _globalQueue = new NotifyingQueue<object>();
                foreach (var source in _sources)
                {
                    source.Queue = _globalQueue;
                }
            }
            else
            {
                foreach (var source in _sources.Skip(1))
                {
                    source.Queue = new NotifyingQueue<object>();
                }
                _globalQueue = null;
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
                    InitQueues();
                }
            }
        }

        public NodeOutput(Type valueType, bool exclusiveMode = true)
        {
            _exclusiveModeEnabled = exclusiveMode;
            _valueType = valueType;
            InitQueues();
        }

        public void Push(object value)
        {
            lock (_lockGuard)
            {
                if (_exclusiveModeEnabled)
                {
                    _globalQueue.Enqueue(value);
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

        internal IValueSource TakeValueSource()
        {
            lock (_lockGuard)
            {
                NotifyingQueue<object> queue;
                if (_exclusiveModeEnabled)
                {
                    queue = _globalQueue;
                }
                else
                {
                    queue = new NotifyingQueue<object>();
                }

                var source = new OutputSource(_valueType, queue);
                _sources.Add(source);
                return source;
            }
        }
    }
}
