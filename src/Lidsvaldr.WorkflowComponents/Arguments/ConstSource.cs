using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    public sealed class ConstSource<T> : AbstractValueSource<T>
    {
        #region private fileds
        private readonly T _value;
        private readonly bool _exhaustible;
        private bool _exhausted;
        #endregion private fields

        #region public fields
        public override bool IsExhausted => _exhausted;

        public override bool IsValueReady => !_exhausted;

        public override event Action<IValueSource> ValueReady;
        #endregion public fields

        #region public methods
        public ConstSource(T value, bool exhaustible = true)
        {
            _value = value;
            _exhaustible = exhaustible;
            _exhausted = false;
        }

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
        #endregion public methods
    }
}
