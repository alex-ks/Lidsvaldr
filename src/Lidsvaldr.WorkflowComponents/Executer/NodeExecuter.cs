using Lidsvaldr.WorkflowComponents.Arguments;
using Lidsvaldr.WorkflowComponents.Contracts;
using Lidsvaldr.WorkflowComponents.Utility;
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
        private NodeArgumentArray<NodeInput> _inputs;
        private NodeArgumentArray<NodeOutput> _outputs;
        private readonly object _lockGuard = new object();
        private int _threadLimit;
        #endregion private fields

        #region public fields
        public Delegate function { get; private set; }

        public bool IsInputReady { get { return (Inputs != null && Inputs.All(x => x.ValueReady)); } }

        public bool IsOutputLock { get { return (Outputs == null || Outputs.Any(x => x.IsLocked)); } }

        public NodeArgumentArray<NodeInput> Inputs
        {
            get { return _inputs; }
            private set
            {
                if (value == null)
                    throw new ArgumentNullException();
                _inputs = value;
            }
        }

        public NodeArgumentArray<NodeOutput> Outputs
        {
            get { return _outputs; }
            private set
            {
                if (value == null)
                    throw new ArgumentNullException();
                _outputs = value;
            }
        }

        public int ThreadLimit {
            get { return _threadLimit; }
            set {
                lock (_lockGuard)
                {
                    if (value == _threadLimit)
                        return;
                    _threadLimit = value;
                }
            }
        }
        #endregion public fields

        #region public methods
        //public NodeExecuter(Func<IValueSource[], IValueSource[]> function)
        //{
        //    this.function = function;
        //}

        public NodeExecuter(Delegate d, int threadLimit = 1)
        {
            _threadLimit = threadLimit;
            function = d;
            var method = d.Method;
            var parameters = method.GetParameters();

            _inputs = new NodeArgumentArray<NodeInput>(parameters.Where(p => !p.IsOut).Select(p => new NodeInput(p.GetType())).ToArray());
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
            _outputs = new NodeArgumentArray<NodeOutput>(outputs.ToArray());
        }

        public void Execute()
        {
            if (!IsInputReady || IsOutputLock)
                return;
            lock (_lockGuard)
            {
                var parameters = Inputs.Select(i =>
                {
                    object obj;
                    i.TryGetValue(out obj);
                    return obj;
                }).ToList();
                var outParameters = (Outputs.Any()) ? Enumerable.Repeat(new object(), Outputs.Count() - 1).ToArray() : Enumerable.Empty<object>();
                parameters.AddRange(outParameters);
                var result = function.Method.Invoke(this, parameters.ToArray());
                for (int i = 0; i < outParameters.Count(); i++)
                {
                    //TODO cast parameters?
                    Outputs[i].Push(parameters[i + Inputs.Count()]);
                }
                if (function.Method.ReturnType != typeof(void))
                {
                    Outputs.Last().Push(result);
                }
            }
        }
        #endregion public methods
    }
}
