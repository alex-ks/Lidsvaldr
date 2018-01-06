using System;
using System.Collections.Generic;
using System.Text;
using Lidsvaldr.WorkflowComponents.Abstracts;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    /// <summary>
    /// Input source entity that contains enumerable value.
    /// </summary>
    /// <typeparam name="T">Type of collection element.</typeparam>
    public class EnumerableSource<T> : AbstractInputValueSource<T>
    {
        /// <summary>
        /// Source value enumerator.
        /// </summary>
        private readonly IEnumerator<T> _enumerator;
        /// <summary>
        /// Indicates whether source value enumerator has next value. 
        /// </summary>
        private bool _hasNext;
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
        /// <param name="enumerable">Enumerable source value.</param>
        /// <param name="exhaustible">Configures whether source will be exhaustible.</param>
        public EnumerableSource(IEnumerable<T> enumerable, bool exhaustible = true)
        {
            _exhaustible = exhaustible;
            _exhausted = false;

            _enumerator = enumerable.GetEnumerator();
            _hasNext = _enumerator.MoveNext();
        }

        /// <summary>
        /// Tries to get value from collection and return success status. 
        /// Notifies if next value is ready. 
        /// Resets enumerator if source is inexhaustible and last element of collection is already reached.
        /// </summary>
        /// <param name="value">Output parameter for pulled value.</param>
        /// <returns>True if value was successfully extracted or false otherwise.</returns>
        public override bool Pull(out T value)
        {
            if (_hasNext)
            {
                value = _enumerator.Current;
                _hasNext = _enumerator.MoveNext();
                if (_hasNext || !_exhaustible)
                {
                    ValueReady?.Invoke(this);
                }
                return true;
            }
            else
            {
                if (!_exhaustible) {
                    _enumerator.Reset();
                    _hasNext = _enumerator.MoveNext();
                    return Pull(out value);
                }
                else
                {
                    _exhausted = true;
                    value = default(T);
                    return false;
                }
            }
        }
    }
}
