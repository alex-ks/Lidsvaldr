using System;
using System.Threading.Tasks;

namespace Lidsvaldr.WorkflowComponents
{
    public interface IValueSource
    {
        bool Pull(out object value);
        
        bool IsExhausted { get; }
        bool IsValueReady { get; }
        Type ValueType { get; }

        event Action<IValueSource> ValueReady;
    }
}