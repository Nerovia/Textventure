using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Textventure.Resources
{
    public enum HorizontalAlignment
    {
        Left,
        Center,
        Right,
    }

    public enum VerticalAlignment
    {
        Top,
        Center,
        Bottom,
    }
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

      

        public static Rectangle Margin(this Rectangle instance, int width, int height)
        {
            instance.Inflate(-width, -height);
            return instance;
        }

        public static Rectangle Margin(this Rectangle instance, Size size)
        {
            return instance.Margin(size.Width, size.Height);
        }

        public static Rectangle Crop(this Rectangle instance, Size size, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
        {
            return instance.Crop(size.Width, size.Height, horizontalAlignment, verticalAlignment);
        }

        public static Rectangle Crop(this Rectangle instance, int width, int height, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
        {
            return instance.Crop(width, horizontalAlignment).Crop(height, verticalAlignment);
        }

        public static Rectangle Crop(this Rectangle instance, int height, VerticalAlignment alignment)
        {
            var _height = instance.Height;
            instance.Height = height;
            switch (alignment)
            {
                case VerticalAlignment.Center:
                    instance.Y += (_height - height) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    instance.Y += _height - height;
                    break;
            }
            return instance;
        }

        public static Rectangle Crop(this Rectangle instance, int width, HorizontalAlignment alignment)
        {
            var _width = instance.Width;
            instance.Width = width;
            switch (alignment)
            {
                case HorizontalAlignment.Center:
                    instance.X += (_width - width) / 2;
                    break;
                case HorizontalAlignment.Right:
                    instance.X += _width - width;
                    break;
            }
            return instance;
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
