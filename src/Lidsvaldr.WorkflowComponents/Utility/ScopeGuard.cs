using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Utility
{
    public sealed class ScopeGuard : IDisposable
    {
        private readonly Action _cleanup;
        private bool _released = false;

        public ScopeGuard(Action action)
        {
            _cleanup = action;
        }

        public void Dispose()
        {
            if (!_released)
            {
                _released = true;
                _cleanup();
            }
        }

        public void Release()
        {
            _released = true;
        }
    }
}
