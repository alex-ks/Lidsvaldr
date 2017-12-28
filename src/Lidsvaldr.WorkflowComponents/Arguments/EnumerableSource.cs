using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    public class EnumerableSource<T> : AbstractValueSource<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private bool _hasNext;
        private bool _exhaustible;
        private bool _exhausted;

        public override bool IsExhausted => /*!_hasNext &&*/ _exhausted;

        public override bool IsValueReady => /*_hasNext &&*/ !_exhausted;

        public bool Exhaustible
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

        public override event Action<IValueSource> ValueReady;

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
