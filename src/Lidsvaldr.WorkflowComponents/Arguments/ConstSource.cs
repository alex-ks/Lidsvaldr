using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    public sealed class ConstSource<T> : AbstractValueSource<T>
    {
        public override bool IsExhausted => _exhausted;

        public override bool IsValueReady => _exhausted;

        public override event Action<IValueSource> ValueReady;

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

        private readonly T _value;
        private readonly bool _exhaustible;
        private bool _exhausted;

        public ConstSource(T value, bool exhaustible = true)
        {
            _value = value;
            _exhaustible = exhaustible;
            _exhausted = false;
        }
    }
}
