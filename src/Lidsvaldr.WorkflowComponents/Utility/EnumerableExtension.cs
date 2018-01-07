using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lidsvaldr.WorkflowComponents.Utility
{
    internal static class EnumerableExtension
    {
        public static IEnumerable<T> RandomShuffle<T>(this IEnumerable<T> collection)
        {
            var array = collection.ToArray();
            var rng = new Random();
            
            for (int i = 0; i < array.Length; ++i)
            {
                var index = rng.Next(i, array.Length);
                yield return array[index];
                (array[index], array[i]) = (array[i], array[index]);
            }
        }
    }
}
