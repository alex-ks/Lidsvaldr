using Lidsvaldr.WorkflowComponents.Arguments;
using Lidsvaldr.WorkflowComponents.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lidsvaldr.WorkflowComponents.Executer
{
    public class NodeExecuter : INodeExecuter
    {
        public Func<IValueSource[], IValueSource[]> function { get; private set; }

        public bool IsInputReady { get { return (Inputs != null && Inputs.All(x => x.IsValueReady)); } }

        private IValueSource[] _inputs;

        public IValueSource[] Inputs
        {
            get { return _inputs; }
            private set
            {
                if (value == null)
                    throw new ArgumentNullException();
                if (_inputs != null && value.Length != _inputs.Length)
                    throw new ArgumentException();
                if (_inputs == null)
                {
                    _inputs = value;
                }
                for (int i = 0; i < value.Length; i++)
                {
                    _inputs[i].Push(value[i]);
                }
            }
        }

        private IValueSource[] _outputs;

        public IValueSource[] Outputs
        {
            get { return _outputs; }
            private set
            {
                if (value == null)
                    throw new ArgumentNullException();
                if (_outputs != null && value.Length != _outputs.Length)
                    throw new ArgumentException();
                if (_outputs == null)
                {
                    _outputs = value;
                }
                for (int i = 0; i < value.Length; i++)
                {
                    _outputs[i].Push(value[i]);
                }
            }
        }

        public NodeExecuter(Func<IValueSource[], IValueSource[]> function)
        {
            this.function = function;
        }

        public async Task Execute()
        {
            while (!IsInputReady)
            {
                await Task.Delay(500);
            }
            Outputs = function(Inputs);
        }
    }
}
