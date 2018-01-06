using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    public static class InputExtensions
    {
        public static void Add(this NodeInput input, NodeOutput output)
        {
            input.AddSource(output.TakeValueSource());
        }

        public static void Add<T>(this NodeInput input, T constant, bool exhaustible = true)
        {
            input.AddSource(new ConstSource<T>(constant, exhaustible));
        }

        public static void Add<T>(this NodeInput input, IEnumerable<T> collection, bool exhaustible = true)
        {
            input.AddSource(new EnumerableSource<T>(collection, exhaustible));
        }
    }
}
