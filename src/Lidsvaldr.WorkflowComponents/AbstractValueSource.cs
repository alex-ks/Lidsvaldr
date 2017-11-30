using System;

namespace Lidsvaldr.WorkflowComponents
{
    public abstract class AbstractValueSource<T> : IValueSource
    {
        public abstract bool IsExhausted { get; }
        public abstract bool IsValueReady { get; }
        public abstract Type ValueType { get; }

        public abstract event Action<IValueSource> ValueReady;

        public abstract T Pull();

        object IValueSource.Pull()
        {
            return Pull();
        }
    }
}