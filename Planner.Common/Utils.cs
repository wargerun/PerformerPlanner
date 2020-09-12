using System;
using System.Collections.Generic;

namespace Planner.Common
{
    public static class Utils
    {
        public static bool IsNullOrEmpty(this string text) => string.IsNullOrWhiteSpace(text);

        public static bool IsNotNullOrEmpty(this string text) => !text.IsNullOrEmpty();

        public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            items.ForEach((item, _) => action(item));
        }

        public static void ForEach<T>(this IEnumerable<T> items, Action<T, int> action)
        {
            int count = 0;

            foreach (T item in items)
            {
                action(item, count);
                count++;
            }
        }
    }
}
