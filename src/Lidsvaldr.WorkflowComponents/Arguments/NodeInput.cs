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
        private bool _valueReady = false;
        private volatile bool _capturingValue = false;
        private object _capturedValue = null;
        private int _lastValueSource = 0;

        public bool ValueReady => _valueReady && !_capturingValue;
        public event Action ValueCaptured;

        public NodeInput(Type t)
        {
            _type = t;
        }

        internal void AddSource(IValueSource source)
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

        public void Add(NodeOutput source)
        {
            AddSource(source.TakeValueSource());
        }

        public void Add<T>(T constant, bool exhaustible = true)
        {
            AddSource(new ConstSource<T>(constant, exhaustible));
        }

        public void Add<T>(IEnumerable<T> collection, bool exhaustible = true)
        {
            AddSource(new EnumerableSource<T>(collection, exhaustible));
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
                    if (_lastValueSource == _sources.Count)
                    {
                        _lastValueSource = 0;
                    }
                    if (_sources.Count != 0)
                    {
                        TryCaptureValue(_sources.Skip(_lastValueSource).First());
                    }
                    return true;
                }
                else
                {
                    return false;
                }
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
                            source.ValueReady -= TryCaptureValue;
                            _sources.Remove(source);
                        }
                        if (_valueReady)
                        {
                            ValueCaptured?.Invoke();
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
