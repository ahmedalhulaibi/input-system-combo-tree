using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class ExtensionMethods
{

    public static void Each<T>(this IEnumerable<T> ie, Action<T, int> action)
    {
        var i = 0;
        foreach (var e in ie) action(e, i++);
    }

}