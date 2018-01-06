using Lidsvaldr.WorkflowComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    /// <summary>
    /// Represents an input entity for node entity.
    /// </summary>
    public sealed class NodeInput
    {
        /// <summary>
        /// Type of input value.
        /// </summary>
        private readonly Type _type;
        /// <summary>
        /// Value source list.
        /// </summary>
        private readonly List<IValueSource> _sources = new List<IValueSource>();
        /// <summary>
        /// Mutex.
        /// </summary>
        private readonly object _lockGuard = new object();
        /// <summary>
        /// Configures whether input value is ready.
        /// </summary>
        private volatile bool _valueReady = false;
        /// <summary>
        /// Indicates whether current entity is trying to capture source value at that moment.
        /// </summary>
        private volatile bool _capturingValue = false;
        /// <summary>
        /// Represents current input value.
        /// </summary>
        private object _capturedValue = null;

		/// <summary>
        /// Type of input value.
        /// </summary>		
        internal Type ValueType => _type;
        /// <summary>
        /// Indicates whether input value is ready.
        /// </summary>        
        public bool ValueReady => _valueReady;
        /// <summary>
        /// Event to notify the value capturing.
        /// </summary>
        public event Action ValueCaptured;

        /// <summary>
        /// Indicates whether ValueCaptured event occurred. Used for silenced mode.
        /// </summary>
        private bool _eventOccurred = false;
        /// <summary>
        /// Indicates whether current entity will raised events.
        /// </summary>
        private bool _silenced = false;
        /// <summary>
        /// Configures whether current entity will raised events.
        /// </summary>
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

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="t">Type of input value.</param>
        public NodeInput(Type t)
        {
            _type = t;
        }

        /// <summary>
        /// Adds specified source to list of sources for current input entity.
        /// </summary>
        /// <param name="source">Value source entity.</param>
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

        /// <summary>
        /// Tries to take value from one of ready sources and returns success status.
        /// </summary>
        /// <param name="value">Output parameter for value.</param>
        /// <returns>True if value was ready or false otherwise.</returns>
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

        /// <summary>
        /// Removes specified source from list of sources for current input entity.
        /// </summary>
        /// <param name="source">Source to remove.</param>
        private void RemoveSource(IValueSource source)
        {
            source.ValueReady -= TryCaptureValue;
            _sources.Remove(source);
        }

        /// <summary>
        /// Raises ValueCaptured event.
        /// </summary>
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

        /// <summary>
        /// Tries to capture value from specified source in a thread-safety way.
        /// </summary>
        /// <param name="source">Source to value capture.</param>
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
