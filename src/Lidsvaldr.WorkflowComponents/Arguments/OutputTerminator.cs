using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    public class OutputTerminator<T> : IEnumerable<T>
    {
        private readonly List<IValueSource> _sources;
        private readonly object _lockGuard = new object();
        private readonly List<T> results;

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
                source.ValueReady += (s) =>
                {
                    if (s.Pull(out object value))
                    {
                        lock (_lockGuard)
                        {
                            results.Add((T)value);
                        }
                    }
                };
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_lockGuard)
            {
                foreach (var val in results)
                {
                    yield return val;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
