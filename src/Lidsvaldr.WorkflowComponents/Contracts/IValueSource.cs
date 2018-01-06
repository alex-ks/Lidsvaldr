using System;
using System.Threading.Tasks;

namespace Lidsvaldr.WorkflowComponents
{
    /// <summary>
    /// Represents a base interface for input source entity.
    /// </summary>
    public interface IValueSource
    {
        /// <summary>
        /// Tries to get value and return success status. 
        /// </summary>
        /// <param name="value">Output parameter for pulled value.</param>
        /// <returns>True if value was successfully extracted or false otherwise.</returns>
        bool Pull(out object value);
        
        /// <summary>
        /// Indicates whether source value is exhaused.
        /// </summary>
        bool IsExhausted { get; }
        /// <summary>
        /// Indicates whether source value is ready to be pulled.
        /// </summary>
        bool IsValueReady { get; }
        /// <summary>
        /// Type of source value.
        /// </summary>
        Type ValueType { get; }

        /// <summary>
        /// Event to notify the value readiness.
        /// </summary>
        event Action<IValueSource> ValueReady;
    }
}