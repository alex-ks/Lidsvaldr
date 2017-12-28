using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Utility
{
    public class NodeArgumentArray<T> : IEnumerable<T>
    {
        private T[] _array;

        public T this[int index]
        {
            get { return _array[index]; }
        }

        public NodeArgumentArray(T[] array)
        {
            _array = array;
        }

        public int Length => _array.Length;

        public IEnumerator<T> GetEnumerator()
        {
            foreach (T t in _array)
            {
                if (t == null)
                {
                    break;
                }

                yield return t;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
