using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    public class EnumerableSource<T> : AbstractValueSource<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private bool _hasNext;

        public override bool IsExhausted => _hasNext;

        public override bool IsValueReady => _hasNext;

        public override event Action<IValueSource> ValueReady;

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
                value = default(T);
                return false;
            }
        }

        public EnumerableSource(IEnumerable<T> enumerable)
        {
            _enumerator = enumerable.GetEnumerator();
            _hasNext = _enumerator.MoveNext();
        }
    }
}
