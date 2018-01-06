using System;
using System.Threading.Tasks;

namespace Lidsvaldr.WorkflowComponents.Abstracts
{
    /// <summary>
    /// Represents an abstract class for input source entity.
    /// </summary>
    /// <typeparam name="T">Type of source value.</typeparam>
    public abstract class AbstractInputValueSource<T> : IValueSource
    {
        /// <summary>
        /// Indicates whether source value is exhausted.
        /// </summary>
        public abstract bool IsExhausted { get; }
        /// <summary>
        /// Indicates whether source value is ready to be pulled.
        /// </summary>
        public abstract bool IsValueReady { get; }
        /// <summary>
        /// Configures whether source will be exhaustible.
        /// </summary>
        public abstract bool Exhaustible { get; set; }
        /// <summary>
        /// Type of source value.
        /// </summary>
        Type IValueSource.ValueType { get; } = typeof(T);

        /// <summary>
        /// Event to notify the value readiness.
        /// </summary>
        public abstract event Action<IValueSource> ValueReady;

        /// <summary>
        /// Tries to get value and return success status. 
        /// </summary>
        /// <param name="value">Output parameter for pulled value.</param>
        /// <returns>True if value was successfully extracted or false otherwise.</returns>
        public abstract bool Pull(out T value);

        bool IValueSource.Pull(out object value)
        {
            var succeeded = Pull(out T val);
            value = val;
            return succeeded;
        }
    }
}