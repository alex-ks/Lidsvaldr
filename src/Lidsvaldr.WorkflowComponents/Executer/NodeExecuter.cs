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
        #region private fields
        private NodeInput[] _inputs;
        private NodeOutput[] _outputs;
        #endregion private fields

        #region public fields
        public Delegate function { get; private set; }

        public bool IsInputReady { get { return (Inputs != null && Inputs.All(x => x.ValueReady)); } }

        public bool IsOutputLock { get { return (Outputs == null || Outputs.Any(x => x.IsLocked)); } }

        public NodeInput[] Inputs
        {
            get { return _inputs; }
            private set
            {
                if (value == null)
                    throw new ArgumentNullException();
                if (_inputs != null && value.Length != _inputs.Length)
                    throw new ArgumentException();
                _inputs = value;
            }
        }

        public NodeOutput[] Outputs
        {
            get { return _outputs; }
            private set
            {
                if (value == null)
                    throw new ArgumentNullException();
                if (_outputs != null && value.Length != _outputs.Length)
                    throw new ArgumentException();
                _outputs = value;
            }
        }
        #endregion public fields

        #region public methods
        //public NodeExecuter(Func<IValueSource[], IValueSource[]> function)
        //{
        //    this.function = function;
        //}

        public NodeExecuter(Delegate d)
        {
            function = d;
            var method = d.Method;
            var parameters = method.GetParameters();

            _inputs = parameters.Where(p => !p.IsOut).Select(p => new NodeInput(p.GetType())).ToArray();
            if (_inputs.Count() == 0)
            {
                throw new ArgumentException(ComponentsResources.InvalidInputDelegate);
            }
            foreach (var input in _inputs)
            {
                input.ValueCaptured += Execute;
            }

            var outputs = Enumerable.Empty<NodeOutput>().ToList();
            outputs.AddRange(parameters.Where(p => p.IsOut).Select(p => new NodeOutput(p.GetType())));
            if (method.ReturnType != typeof(void))
            {
                outputs.Add(new NodeOutput(method.ReturnType));
            }
            _outputs = outputs.ToArray();
        }

        //TODO does it needs an indexer for nodeOutput?
        public NodeInput this[int index]
        {
            get {
                return this.Inputs[index];
            }
            private set { }
        }

        public void Execute()
        {
            if (!IsInputReady || IsOutputLock)
                return;
            var parameters = Inputs.Select(i => {
                object obj;
                i.TryGetValue(out obj);
                return obj;
            }).ToList();
            var outParameters = (Outputs.Any()) ? Enumerable.Repeat(new object(), Outputs.Count() - 1).ToArray() : Enumerable.Empty<object>();
            parameters.AddRange(outParameters);
            var result = function.Method.Invoke(this, parameters.ToArray());
            for(int i = 0; i < outParameters.Count(); i++)
            {
                //TODO cast parameters?
                Outputs[i].Push(parameters[i + Inputs.Count()]);
            }
            if (function.Method.ReturnType != typeof(void))
            {
                Outputs.Last().Push(result);
            }
        }
        #endregion public methods
    }
}
