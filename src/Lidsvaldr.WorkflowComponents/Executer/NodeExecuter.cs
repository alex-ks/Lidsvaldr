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
    public class NodeExecuter
    {
        private NodeArgumentArray<NodeInput> _inputs;
        private NodeArgumentArray<NodeOutput> _outputs;
        private readonly object _lockGuard = new object();
        private volatile int _threadLimit;

        private Dictionary<Guid, (DateTime launchTime, List<Action> finishedCallbacks)> _activeTasks
            = new Dictionary<Guid, (DateTime launchTime, List<Action> finishedCallbacks)>();

        public Delegate Function { get; private set; }

        private bool IsInputReady { get { return (Inputs != null && Inputs.All(x => x.ValueReady)); } }

        private bool IsOutputLock { get { return (Outputs == null || Outputs.Any(x => x.IsLocked)); } }

        public bool IsBusy => _activeTasks.Count >= ThreadLimit;

        public string Name { get; }

        public NodeArgumentArray<NodeInput> Inputs
        {
            get { return _inputs; }
            private set { _inputs = value ?? throw new ArgumentNullException(); }
        }

        public NodeArgumentArray<NodeOutput> Outputs
        {
            get { return _outputs; }
            private set { _outputs = value ?? throw new ArgumentNullException(); }
        }

        public int ThreadLimit
        {
            get { return _threadLimit; }
            set
            {
                lock (_lockGuard)
                {
                    if (value == _threadLimit)
                        return;
                    _threadLimit = value;
                }
            }
        }

        public event Action<Exception> ExceptionOccurred;

        public NodeExecuter(Delegate d, int threadLimit = 1, string name = null)
        {
            if (threadLimit <= 0)
            {
                throw new ArgumentException(ComponentsResources.ThreadLimitMustBePositive, nameof(threadLimit));
            }

            _threadLimit = threadLimit;
            Function = d;
            var method = d.Method;
            var parameters = method.GetParameters();

            _inputs = new NodeArgumentArray<NodeInput>(parameters
                                                           .Where(p => !p.IsOut)
                                                           .Select(p => new NodeInput(p.ParameterType)).ToArray());
            if (_inputs.Length == 0)
            {
                throw new ArgumentException(ComponentsResources.InvalidInputDelegate);
            }
            foreach (var input in _inputs)
            {
                input.ValueCaptured += () => TryExecute();
            }

            var outputs = Enumerable.Empty<NodeOutput>().ToList();
            outputs.AddRange(parameters.Where(p => p.IsOut).Select(p => new NodeOutput(p.ParameterType)));
            if (method.ReturnType != typeof(void))
            {
                outputs.Add(new NodeOutput(method.ReturnType));
            }
            _outputs = new NodeArgumentArray<NodeOutput>(outputs.ToArray());

            foreach (var output in _outputs) { ExceptionOccurred += output.NotifyAboutException; }

            Name = name ?? GenerateNodeName();
        }

        private string GenerateNodeName()
        {
            var inputsPart = String.Join(" * ", Inputs.Select(x => x.ValueType.Name));
            var outputsPart = String.Join(" * ", Outputs.Select(x => x.ValueType.Name));
            return $"{inputsPart} -> {outputsPart}";
        }

        private bool TryExecute()
        {
            lock (_lockGuard)
            {
                if (!IsInputReady || IsBusy)
                    return false;

                var taskId = Guid.NewGuid();
                var launchTime = DateTime.Now;

                try
                {
                    _activeTasks.Add(taskId, (launchTime, new List<Action>()));

                    foreach (var i in Inputs) { i.Silenced = true; }

                    var parameters = Inputs.Select(i =>
                    {
                        i.TryTakeValue(out object obj);
                        return obj;
                    }).ToList();

                    var outParameters = (Outputs.Any()) ?
                    Enumerable.Repeat(new object(), Outputs.Count() - 1).ToArray() :
                    Enumerable.Empty<object>().ToArray();
                    parameters.AddRange(outParameters);

                    Task.Factory.StartNew(() => ExecuteTask(
                        taskId,
                        launchTime,
                        parameters.ToArray(),
                        outParameters.Length));

                    return true;
                }
                catch (Exception e)
                {
                    _activeTasks.Remove(taskId);
                    ExceptionOccurred(e);
                    return false;
                }
                finally
                {
                    foreach (var i in Inputs) { i.Silenced = false; }
                }
            }
        }

        private void ExecuteTask(Guid taskId, DateTime launchTime, object[] parameters, int outParametersCount)
        {
            try
            {
                var result = Function.DynamicInvoke(parameters);

                lock (_lockGuard)
                {
                    var needToWait = new List<(NodeOutput output, object value)>();

                    void FillOutputs()
                    {
                        for (int i = 0; i < outParametersCount; i++)
                        {
                            var output = Outputs[i];
                            var value = parameters[i + Inputs.Length];
                            if (!output.TryPush(value))
                            {
                                if (output.DiscardIfLocked)
                                    continue;
                                needToWait.Add((output, value));
                            }
                        }
                        if (Function.Method.ReturnType != typeof(void))
                        {
                            var output = Outputs[Outputs.Length - 1];
                            if (!output.TryPush(result))
                            {
                                if (!output.DiscardIfLocked)
                                {
                                    needToWait.Add((output, result));
                                }
                            }
                        }
                        void PushWhenUnlocked()
                        {
                            lock (_lockGuard)
                            {
                                if (needToWait.Any(x => x.output.IsLocked))
                                    return;
                                foreach (var (output, value) in needToWait)
                                {
                                    output.TryPush(value);
                                    output.OutputUnlocked -= PushWhenUnlocked;
                                }
                                FinishExection(taskId);
                            }
                        }
                        if (needToWait.Count != 0)
                        {
                            foreach (var (output, value) in needToWait)
                            {
                                output.OutputUnlocked += PushWhenUnlocked;
                            }
                        }
                        else
                        {
                            FinishExection(taskId);
                        }
                    }

                    var predcessor = _activeTasks.Values
                        .OrderBy(x => x.launchTime)
                        .LastOrDefault(x => x.launchTime < launchTime);

                    if (predcessor.finishedCallbacks != null)
                    {
                        predcessor.finishedCallbacks.Add(FillOutputs);
                    }
                    else
                    {
                        FillOutputs();
                    }
                }
            }
            catch (Exception e)
            {
                ExceptionOccurred(new ExecutionException(Name, parameters, e));
            }
        }

        private void FinishExection(Guid taskId)
        {
            lock (_lockGuard)
            {
                var (time, callbacks) = _activeTasks[taskId];
                _activeTasks.Remove(taskId);
                foreach (var callback in callbacks)
                {
                    callback();
                }
                TryExecute();
            }
        }
    }
}
