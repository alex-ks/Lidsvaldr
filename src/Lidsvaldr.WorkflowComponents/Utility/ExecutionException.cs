using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Utility
{
    public class ExecutionException : Exception
    {
        private string _sourceName;
        private object[] _args;

        public string SourceName => _sourceName;
        public object[] ExecutionArguments => _args;

        internal ExecutionException(string source, object[] args, Exception inner) 
            : base(ComponentsResources.ExecutionException, inner)
        {
            _sourceName = source;
            _args = args;
        }
    }
}
