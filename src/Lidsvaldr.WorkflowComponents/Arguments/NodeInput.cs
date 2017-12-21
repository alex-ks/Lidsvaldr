using Lidsvaldr.WorkflowComponents.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    public sealed class NodeInput
    {
        private readonly Type _type;
        private readonly List<IValueSource> _sources;
        private readonly object _lockGuard = new object();

        private bool _valueReady = false;
        private volatile bool _capturingValue = false;
        private object _capturedValue = null;

        // With this implementation there is a risk of spamming by quickly reloading source
        private void TryCaptureValue(IValueSource source)
        {
            lock (_lockGuard)
            {
                if (_capturingValue)
                    return;
                _capturingValue = true;
                using (new ScopeGuard(() => _capturingValue = false))
                {
                    if (!_valueReady)
                    {
                        _valueReady = source.Pull(out _capturedValue);
                        if (_valueReady)
                        {
                            ValueCaptured?.Invoke();
                        }
                    }
                }
            }
        }

        public void Add(IValueSource source)
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

        public bool TryGetValue(out object value)
        {
            lock (_lockGuard)
            {
                value = _capturedValue;
                if (_valueReady)
                {
                    _capturedValue = null;
                    _valueReady = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool ValueReady => _valueReady;

        public event Action ValueCaptured;
    }
}
