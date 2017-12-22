using System;
using System.Threading.Tasks;

namespace Lidsvaldr.WorkflowComponents
{
    public abstract class AbstractValueSource<T> : IValueSource
    {
        public abstract bool IsExhausted { get; }
        public abstract bool IsValueReady { get; }
        Type IValueSource.ValueType { get; } = typeof(T);

        public abstract event Action<IValueSource> ValueReady;

        public abstract bool Pull(out T value);

        bool IValueSource.Pull(out object value)
        {
            var succeeded = Pull(out T val);
            value = val;
            return succeeded;
        }
    }
}