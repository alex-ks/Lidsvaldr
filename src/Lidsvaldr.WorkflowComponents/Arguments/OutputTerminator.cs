using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    /// <summary>
    /// Entity for extraction output value from node.
    /// </summary>
    /// <typeparam name="T">Output value type.</typeparam>
    public class OutputTerminator<T> : IEnumerable<T>
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

            lock (_lockGuard)
            {
                var source = output.TakeValueSource();
                source.ValueReady += TryTakeValue;
                TryTakeValue(source);
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
                    TryTakeValue(source);
                }
            }
        }

        /// <summary>
        /// Gets typed enumerator of terminator results collection.
        /// </summary>
        /// <returns>Typed enumerator of termitaror results collection.</returns>
        public IEnumerator<T> GetEnumerator()
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
        /// Gets enumerator of terminator results collection.
        /// </summary>
        /// <returns>Enumerator of termitaror results collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
