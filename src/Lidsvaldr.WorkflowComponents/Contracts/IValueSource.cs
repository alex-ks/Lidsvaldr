using System;
using System.Threading.Tasks;

namespace Lidsvaldr.WorkflowComponents
{
    public interface IValueSource
    {
        Task<object> Pull();
        Task Push(IValueSource item);
        bool IsExhausted { get; }
        bool IsValueReady { get; }
        Type ValueType { get; }

        event Action<IValueSource> ValueReady;
    }
}