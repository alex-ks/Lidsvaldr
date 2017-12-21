using System;
using System.Threading.Tasks;

namespace Lidsvaldr.WorkflowComponents
{
    public abstract class AbstractValueSource<T> : IValueSource
    {
        public abstract bool IsExhausted { get; protected set; }
        public abstract bool IsValueReady { get; }
        public abstract Type ValueType { get; }

        public abstract event Action<IValueSource> ValueReady;

        public abstract Task<T> Pull();
        public abstract Task Push(IValueSource item);

        async Task<object> IValueSource.Pull()
        {
            return await Pull();
        }
        async Task IValueSource.Push(IValueSource item)
        {
            await Push(item);
        }
    }
}