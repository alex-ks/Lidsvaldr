using System;
using System.Collections.Generic;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Arguments
{
    /// <summary>
    /// Implements an extension methods for NodeInput entity.
    /// </summary>
    public static class InputExtensions
    {
        /// <summary>
        /// Links NodeInput and NodeOutput entities by adding output source from specified output entity to list of input sources for specified input entity.
        /// </summary>
        /// <param name="input">Node input entity.</param>
        /// <param name="output">Node output entity.</param>
        public static void Add(this NodeInput input, NodeOutput output)
        {
            input.AddSource(output.TakeValueSource());
        }

        /// <summary>
        /// Adds new constant input source to list of input sources for specified input entity.
        /// </summary>
        /// <typeparam name="T">Type of constant value.</typeparam>
        /// <param name="input">Node input entity.</param>
        /// <param name="constant">Value of constant.</param>
        /// <param name="exhaustible">Configures whether source will be exhaustible.</param>
        public static void Add<T>(this NodeInput input, T constant, bool exhaustible = true)
        {
            input.AddSource(new ConstSource<T>(constant, exhaustible));
        }

        /// <summary>
        /// Adds new enumerable input source to list of input sources for specified input entity.
        /// </summary>
        /// <typeparam name="T">Type of collection element.</typeparam>
        /// <param name="input">Node input entity.</param>
        /// <param name="collection">Value of collection.</param>
        /// <param name="exhaustible">Configures whether source will be exhaustible.</param>
        public static void Add<T>(this NodeInput input, IEnumerable<T> collection, bool exhaustible = true)
        {
            input.AddSource(new EnumerableSource<T>(collection, exhaustible));
        }
    }
}
