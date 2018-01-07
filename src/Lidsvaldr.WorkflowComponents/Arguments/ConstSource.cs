using System;
using System.Collections.Generic;
using System.Text;
using Lidsvaldr.WorkflowComponents.Abstracts;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    /// <summary>
    /// Input source entity that contains constant value.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    public sealed class ConstSource<T> : AbstractValueSource<T>
    {
        /// <summary>
        /// Source value.
        /// </summary>
        private readonly T _value;
        /// <summary>
        /// Indicates whether source will be exhaustible.
        /// </summary>
        private bool _exhaustible;
        /// <summary>
        /// Indicates whether source value is exhaused.
        /// </summary>
        private bool _exhausted;

        /// <summary>
        /// Indicates whether source value is exhaused.
        /// </summary>
        public override bool IsExhausted => _exhausted;
        /// <summary>
        /// Indicates whether source value is ready to be pulled.
        /// </summary>
        public override bool IsValueReady => !_exhausted;
        /// <summary>
        /// Configures whether source will be exhaustible.
        /// </summary>
        public override bool Exhaustible
        {
            get { return _exhaustible; }
            set
            {
                if (_exhaustible == value)
                    return;
                _exhaustible = value;
                _exhausted = false;
            }
        }
        /// <summary>
        /// Event to notify the value readiness.
        /// </summary>
        public override event Action<IValueSource> ValueReady;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="value">Source value.</param>
        /// <param name="exhaustible">Configures whether source will be exhaustible.</param>
        public ConstSource(T value, bool exhaustible = true)
        {
            _value = value;
            _exhaustible = exhaustible;
            _exhausted = false;
        }

        /// <summary>
        /// Tries to get value and return success status. Notifies if next value is ready.
        /// </summary>
        /// <param name="value">Output parameter for pulled value.</param>
        /// <returns>True if value was successfully extracted or false otherwise.</returns>
        public override bool Pull(out T value)
        {
            if (!_exhausted)
            {
                value = _value;
                if (_exhaustible)
                {
                    _exhausted = true;
                }
                else
                {
                    ValueReady?.Invoke(this);
                }
                return true;
            }
            value = default(T);
            return false;
        }
    }
}
