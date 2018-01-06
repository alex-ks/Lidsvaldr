using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    /// <summary>
    /// Entity for extraction output value from node.
    /// All operations of the terminator are synchronized with internal lock object.
    /// </summary>
    /// <typeparam name="T">Output value type.</typeparam>
    public class OutputTerminator<T>
    {
        /// <summary>
        /// Value sources.
        /// </summary>
        private readonly List<IValueSource> _sources = new List<IValueSource>();
        /// <summary>
        /// Mutex.
        /// </summary>
        private readonly object _lockGuard = new object();
        /// <summary>
        /// Collection of extracted results.
        /// </summary>
        private readonly List<T> _results = new List<T>();
        private readonly List<Exception> _errors = new List<Exception>();

        private Dictionary<Guid, int> _leftToWait = new Dictionary<Guid, int>();

        /// <summary>
        /// Connects specified node output to current terminator.
        /// </summary>
        /// <param name="output">Output entity of node.</param>
        public void Add(NodeOutput output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (output.ValueType != typeof(T))
            {
                throw new ArgumentException(ComponentsResources.InputTypeMismatch);
            }

            output.ExceptionOccurred += RegisterException;

            lock (_lockGuard)
            {
                var source = output.TakeValueSource();
                source.ValueReady += TryTakeValue;
                TryTakeValue(source);
            }
        }

        /// <summary>
        /// Decrements registered all wait counters
        /// </summary>
        private void DecrementCounters()
        {
            foreach (var key in _leftToWait.Keys.ToList())
            {
                var left = --_leftToWait[key];

                if (left <= 0)
                {
                    Monitor.PulseAll(_lockGuard);
                }
            }
        }

        /// <summary>
        /// Adds an exception to error list.
        /// </summary>
        /// <param name="e">Exception to be added</param>
        private void RegisterException(Exception e)
        {
            lock (_lockGuard)
            {
                _errors.Add(e);
                DecrementCounters();
            }
        }

        /// <summary>
        /// Gets all available values from source and push them to results.
        /// </summary>
        /// <param name="source">Node output source.</param>
        private void TryTakeValue(IValueSource source)
        {
            if (source.Pull(out object value))
            {
                lock (_lockGuard)
                {
                    _results.Add((T)value);

                    DecrementCounters();
                    
                    TryTakeValue(source);
                }
            }
        }

        /// <summary>
        /// Waits until specified amount of outputs or exceptions is collected.
        /// Uses specified wait function.
        /// </summary>
        /// <param name="resultsCount">Amount of produced results to be waited</param>
        /// <param name="waitFunction">Function which determines how wait will be performed</param>
        private void GenericWaitForResults(int resultsCount, Action<Func<int>> waitFunction)
        {
            lock (_lockGuard)
            {
                int collectedCount = _results.Count + _errors.Count;
                if (collectedCount >= resultsCount)
                    return;

                var guid = Guid.NewGuid();

                _leftToWait[guid] = resultsCount - collectedCount;

                waitFunction(() => _leftToWait[guid]);

                _leftToWait.Remove(guid);
            }
        }

        /// <summary>
        /// Waits until specified amount of outputs or exceptions is collected.
        /// </summary>
        /// <param name="resultsCount">Amount of produced results to be waited</param>
        public void WaitForResults(int resultsCount)
        {
            GenericWaitForResults(resultsCount, getter =>
            {
                while (getter() > 0)
                {
                    Monitor.Wait(_lockGuard);
                }
            });
        }

        /// <summary>
        /// Waits until specified amount of outputs/exceptions is collected.
        /// If specified time-out interval elapses, returns before the results count is reached.
        /// </summary>
        /// <param name="resultsCount">Amount of produced results to be waited.</param>
        /// <param name="timeout">A TimeSpan representing the amount of time to wait before the return.</param>
        public void WaitForResults(int resultsCount, TimeSpan timeout)
        {
            DateTime start = DateTime.Now;
            GenericWaitForResults(resultsCount, getter =>
            {
                var elapsed = DateTime.Now - start;
                while (getter() > 0 && elapsed < timeout)
                {
                    Monitor.Wait(_lockGuard, timeout - elapsed);
                    elapsed = DateTime.Now - start;
                }
            });
        }

        /// <summary>
        /// Waits until specified amount of outputs/exceptions is collected.
        /// If specified time-out interval elapses, returns before the results count is reached.
        /// </summary>
        /// <param name="resultsCount">Amount of produced results to be waited.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait before the return</param>
        public void WaitForResults(int resultCount, int millisecondsTimeout)
        {
            WaitForResults(resultCount, TimeSpan.FromMilliseconds(millisecondsTimeout));
        }

        /// <summary>
        /// Gets <see cref="IEnumerable{T}"/> with collected results.
        /// Note: terminator methods will be locked until the enumeration ends.
        /// </summary>
        /// <returns>Collection of termitaror results.</returns>
        public IEnumerable<T> Results()
        {
            lock (_lockGuard)
            {
                foreach (var val in _results)
                {
                    yield return val;
                }
            }
        }

        /// <summary>
        /// Gets <see cref="IEnumerable{Exception}"/> with exceptions, occuring during the execution of output's node.
        /// Note: terminator methods will be locked until the enumeration ends.
        /// </summary>
        /// <returns>Collection of termitaror results.</returns>
        public IEnumerable<Exception> Exceptions()
        {
            lock (_lockGuard)
            {
                foreach (var e in _errors)
                {
                    yield return e;
                }
            }
        }
    }
}
