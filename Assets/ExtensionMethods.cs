using System;
using System.Collections.Generic;

namespace ComboTreeInputSystem
{
    public static class ExtensionMethods
    {

        public static void Each<T>(this IEnumerable<T> ie, Action<T, int> action)
        {
            var i = 0;
            foreach (var e in ie) action(e, i++);
        }

    }
}