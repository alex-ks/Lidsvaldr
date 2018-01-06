using Lidsvaldr.WorkflowComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    public sealed class NodeInput
    {
        private readonly Type _type;
        private readonly List<IValueSource> _sources = new List<IValueSource>();
        private readonly object _lockGuard = new object();
        private volatile bool _valueReady = false;
        private volatile bool _capturingValue = false;
        private object _capturedValue = null;

        public bool ValueReady => _valueReady;
        public event Action ValueCaptured;

        private bool _eventOccurred = false;
        private bool _silenced = false;
        internal bool Silenced
        {
            get { return _silenced; }
            set
            {
                if (_silenced == value)
                    return;
                if (_silenced && _eventOccurred && _valueReady)
                    ActivateEvent();
                _silenced = value;
            }
        }

        public NodeInput(Type t)
        {
            _type = t;
        }

        public void AddSource(IValueSource source)
        {
            lock (_lockGuard)
            {
                if (source.ValueType != _type)
                {
                    throw new ArgumentException(ComponentsResources.InputTypeMismatch);
                }
                _sources.Add(source);
                source.ValueReady += TryCaptureValue;
                TryCaptureValue(source);
            }
        }

        public bool TryTakeValue(out object value)
        {
            lock (_lockGuard)
            {
                value = _capturedValue;
                if (_valueReady)
                {
                    _capturedValue = null;
                    _valueReady = false;
                    if (_sources.Count != 0)
                    {
                        var rng = new Random();
                        var ready = _sources.Where(s => s.IsValueReady).ToArray();
                        TryCaptureValue(ready[rng.Next(ready.Length)]);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void RemoveSource(IValueSource source)
        {
            source.ValueReady -= TryCaptureValue;
            _sources.Remove(source);
        }

        private void ActivateEvent()
        {
            if (!Silenced)
            {
                ValueCaptured?.Invoke();
            }
            else
            {
                _eventOccurred = true;
            }
        }

        // With this implementation there is a risk of spamming by quickly reloading source
        private void TryCaptureValue(IValueSource source)
        {
            lock (_lockGuard)
            {
                if (_capturingValue)
                    return;
                _capturingValue = true;
                try
                {
                    if (!_valueReady)
                    {
                        _valueReady = source.Pull(out _capturedValue);
                        if (source.IsExhausted)
                        {
                            RemoveSource(source);
                        }
                        if (_valueReady)
                        {
                            ActivateEvent();
                        }
                    }
                }
                finally
                {
                    _capturingValue = false;
                }
            }
        }
    }
}
