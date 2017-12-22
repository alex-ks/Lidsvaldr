using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    public class EnumerableSource<T> : AbstractValueSource<T>
    {
        #region private fields
        private readonly IEnumerator<T> _enumerator;
        private bool _hasNext;
        private readonly bool _exhaustible;
        private bool _exhausted;
        #endregion private fields

        #region public fields
        public override bool IsExhausted => _hasNext & (!_exhausted);

        public override bool IsValueReady => _hasNext & (!_exhausted);

        public override event Action<IValueSource> ValueReady;
        #endregion public fields

        #region public methods
        public EnumerableSource(IEnumerable<T> enumerable, bool exhaustible = true)
        {
            _exhaustible = exhaustible;
            _exhausted = false;

            _enumerator = enumerable.GetEnumerator();
            _hasNext = _enumerator.MoveNext();
        }

        public override bool Pull(out T value)
        {
            if (_hasNext)
            {
                value = _enumerator.Current;
                _hasNext = _enumerator.MoveNext();
                if (_hasNext)
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
        #endregion public methods
    }
}
