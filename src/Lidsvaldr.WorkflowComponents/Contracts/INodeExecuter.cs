using Lidsvaldr.WorkflowComponents.Arguments;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lidsvaldr.WorkflowComponents.Contracts
{
    public interface INodeExecuter
    {
        Delegate function { get; }
        NodeInput[] Inputs { get; }
        NodeOutput[] Outputs { get; }
        void Execute();
    }
}
