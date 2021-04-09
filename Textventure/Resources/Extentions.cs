using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Textventure.Resources
{
    public static class Extentions
    {
        public static int IndexOf(this String s, string[] words)
        {
            int n = -1;
            foreach(var w in words)
            {
                n = s.IndexOf(w);
                if (n != -1)
                    break;
            }
            return n;
        }

        public static void ForEach<T>(this IEnumerable<T> instance, Action<T> action)
        {
            foreach (T element in instance)
                action(element);
        }

        public static IEnumerable<T> Convert<T, M>(this IEnumerable<M> instance, Func<M, T> morphFunction)
        {
            var t = new List<T>();
            if (instance != null)
                foreach (var m in instance)
                    t.Add(morphFunction(m));
            return t;
        }

        public static string[] SplitTrimmed(this string s, char separator)
        {
            return SplitTrimmed(s, new char[] { separator });
        }

        public static string[] SplitTrimmed(this string s, char[] separators)
        {
            var output = new List<string>();
            var split = s.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var a in split)
            {
                var b = a.Trim();
                if (string.IsNullOrWhiteSpace(b))
                    continue;
                output.Add(b);
            }
            return output.ToArray();
        }
    }
}
