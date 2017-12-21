using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lidsvaldr.WorkflowComponents.Contracts
{
    public interface INodeExecuter
    {
        Func<IValueSource[], IValueSource[]> function { get; }
        IValueSource[] Inputs { get; }
        IValueSource[] Outputs { get; }
        //bool IsInputReady { get; }
        //void SetInput(IValueSource[] input);
        //void SetOutput(IValueSource[] output);
        Task Execute();
    }
}
