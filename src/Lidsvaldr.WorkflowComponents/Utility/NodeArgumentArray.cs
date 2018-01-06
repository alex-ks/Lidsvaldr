using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Utility
{
    /// <summary>
    /// Class for encapsulation of node argument array.
    /// </summary>
    /// <typeparam name="T">Type of collection element.</typeparam>
    public class NodeArgumentArray<T> : IEnumerable<T>
    {
        /// <summary>
        /// Array of node arguments.
        /// </summary>
        private T[] _array;

        /// <summary>
        /// Indexer overloading.
        /// </summary>
        /// <param name="index">Array element index.</param>
        /// <returns></returns>
        public T this[int index]
        {
            get { return _array[index]; }
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="array">Array of node arguments.</param>
        public NodeArgumentArray(T[] array)
        {
            _array = array;
        }

        /// <summary>
        /// Length of node arguments array.
        /// </summary>
        public int Length => _array.Length;

        /// <summary>
        /// Gets typed enumerator of node argument array.
        /// </summary>
        /// <returns>Typed enumerator of node argument array.</returns>
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

        /// <summary>
        /// Gets enumerator of node argument array.
        /// </summary>
        /// <returns>Enumerator of node arguments array.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
