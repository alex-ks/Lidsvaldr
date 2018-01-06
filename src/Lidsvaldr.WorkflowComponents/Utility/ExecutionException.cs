using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Utility
{
    /// <summary>
    /// Represents a custom exception which occuring while node function execution.
    /// </summary>
    public class ExecutionException : Exception
    {
        /// <summary>
        /// Source node name.
        /// </summary>
        private string _sourceName;
        /// <summary>
        /// Arguments which was passed to the node function.
        /// </summary>
        private object[] _args;

        /// <summary>
        /// Source node name.
        /// </summary>
        public string SourceName => _sourceName;
        /// <summary>
        /// Arguments which was passed to the node function.
        /// </summary>
        public object[] ExecutionArguments => _args;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="source">Name of source node.</param>
        /// <param name="args">Arguments which was passed to the node function</param>
        /// <param name="inner">Inner exception.</param>
        internal ExecutionException(string source, object[] args, Exception inner) 
            : base(ComponentsResources.ExecutionException, inner)
        {
            _sourceName = source;
            _args = args;
        }
    }
}
