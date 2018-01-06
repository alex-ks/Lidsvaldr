using Lidsvaldr.WorkflowComponents.Arguments;
using Lidsvaldr.WorkflowComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lidsvaldr.WorkflowComponents.Executer
{
    /// <summary>
    /// Represents a node entity. 
    /// Node has a number of inputs and outputs and can execute some function when inputs and outputs are ready for it.
    /// </summary>
    public class NodeExecuter
    {
        /// <summary>
        /// Collection of node inputs.
        /// </summary>
        private NodeArgumentArray<NodeInput> _inputs;
        /// <summary>
        /// Collection of node outputs.
        /// </summary>
        private NodeArgumentArray<NodeOutput> _outputs;
        /// <summary>
        /// Mutex.
        /// </summary>
        private readonly object _lockGuard = new object();
        /// <summary>
        /// Max number of threads which current node can run at the same time.
        /// </summary>
        private volatile int _threadLimit;

        /// <summary>
        /// Dictionary for tracking active tasks execution.
        /// </summary>
        private Dictionary<Guid, (DateTime launchTime, List<Action> finishedCallbacks)> _activeTasks
            = new Dictionary<Guid, (DateTime launchTime, List<Action> finishedCallbacks)>();

        /// <summary>
        /// Delegate that refers to function which node should execute.
        /// </summary>
        public Delegate Function { get; private set; }

        /// <summary>
        /// Indicates whether all node inputs are ready to give value.
        /// </summary>
        private bool IsInputReady { get { return (Inputs != null && Inputs.All(x => x.ValueReady)); } }

        /// <summary>
        /// Indicates whether all node outputs are ready to take value.
        /// </summary>
        private bool IsOutputLock { get { return (Outputs == null || Outputs.Any(x => x.IsLocked)); } }

        /// <summary>
        /// Indicates if the max number of threads are already runs by current node.
        /// </summary>
        public bool IsBusy => _activeTasks.Count >= ThreadLimit;

        /// <summary>
        /// Name of current node.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Configures node inputs.
        /// </summary>
        public NodeArgumentArray<NodeInput> Inputs
        {
            get { return _inputs; }
            private set { _inputs = value ?? throw new ArgumentNullException(); }
        }

        /// <summary>
        /// Configures node outputs.
        /// </summary>
        public NodeArgumentArray<NodeOutput> Outputs
        {
            get { return _outputs; }
            private set { _outputs = value ?? throw new ArgumentNullException(); }
        }

        /// <summary>
        /// Configures max number of threads which current node can run at the same time.
        /// </summary>
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

        /// <summary>
        /// Event to notify that an exception has occurred.
        /// </summary>
        public event Action<Exception> ExceptionOccurred;

        /// <summary>
        /// Class constructor. Initializes inputs and outputs depending on the specified delegate.
        /// </summary>
        /// <param name="d">Delegate that refers to function which node should execute.</param>
        /// <param name="threadLimit">Max thread count.</param>
        /// <param name="name">Node name.</param>
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

        /// <summary>
        /// Generates new name for current node depending on inputs and outputs types.
        /// </summary>
        /// <returns>Node name.</returns>
        private string GenerateNodeName()
        {
            var inputsPart = String.Join(" * ", Inputs.Select(x => x.ValueType.Name));
            var outputsPart = String.Join(" * ", Outputs.Select(x => x.ValueType.Name));
            return $"{inputsPart} -> {outputsPart}";
        }        
        
        /// <summary>
        /// Tries to run new thread, execute task and returns success status.
        /// </summary>
        /// <returns>True if thread was successfully run or false otherwise.</returns>
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

                    var outParameters = (Outputs.Any())
                        ? Enumerable.Repeat(new object(), Outputs.Count() - 1).ToArray()
                        : Enumerable.Empty<object>().ToArray();
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

        /// <summary>
        /// Executes node function and gives obtained values to node outputs.
        /// </summary>
        /// <param name="taskId">Id of executing task.</param>
        /// <param name="launchTime">Time when task was started to sort tasks.</param>
        /// <param name="parameters">Input values.</param>
        /// <param name="outParametersCount">Number of output values.</param>
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

        /// <summary>
        /// Removes task from tracking list and invokes task's all callbacks.
        /// </summary>
        /// <param name="taskId">>Id of task.</param>
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
