using System;

namespace Lidsvaldr.WorkflowComponents
{
    public interface IValueSource
    {
        object Pull();
        bool IsExhausted { get; }
        bool IsValueReady { get; }
        Type ValueType { get; }

        event Action<IValueSource> ValueReady;
    }
}