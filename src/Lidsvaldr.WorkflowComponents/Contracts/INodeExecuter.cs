using Lidsvaldr.WorkflowComponents.Arguments;
using Lidsvaldr.WorkflowComponents.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lidsvaldr.WorkflowComponents.Contracts
{
    public interface INodeExecuter
    {
        Delegate function { get; }
        NodeArgumentArray<NodeInput> Inputs { get; }
        NodeArgumentArray<NodeOutput> Outputs { get; }
        void Execute();
    }
}
