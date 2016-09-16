using System.Collections.Generic;

namespace ClusterFS.Extension
{
    internal static class ArrayEx
    {
        public static string ToPString<T>(this IEnumerable<T> arr)
        {
            return $"[{string.Join(", ", arr)}]";
        }
    }
}