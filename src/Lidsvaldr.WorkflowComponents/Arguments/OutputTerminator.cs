using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    public class OutputTerminator<T> : IEnumerable<T>
    {
        private readonly List<IValueSource> _sources = new List<IValueSource>();
        private readonly object _lockGuard = new object();
        private readonly List<T> _results = new List<T>();

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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
