using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Textventure.Engine
{
    public enum Speed
    {
        Instant,
        Quick,
        Normal,
        Slow,
    }

    public class ColorOptions
    {
        public ColorOptions()
        {
            foreground = Engine.DefaultForeground;
            background = Engine.DefaultBackground;
        }

        public ConsoleColor foreground;
        public ConsoleColor background;
    }

    public class WriteOptions : ColorOptions
    {
        public bool ignoreScript = false;
        public bool resetSkip = false;
        public bool canSkip = true;
        public bool fill = true;
        public bool showCursor = true;
        public int iteration = 0;
        public Speed speed = Speed.Normal;
    }

    public class BoxOptions : ColorOptions
    {
        public BoxOutline outline = BoxOutline.Single;
        public bool fill = true;
    }

    public static class Engine
    {
        public static Size Size { get; private set; }
        
        public static Rectangle Frame { get; private set; }

        public static bool IsSkipping { get; set; }

        public static bool CanSkip { get; set; }

        public static string Title { get => Console.Title; set => Console.Title = value; }

        public static Random Random { get; } = new Random();

        public static ConsoleColor DefaultForeground { get; set; } = ConsoleColor.Black;

        public static ConsoleColor DefaultBackground { get; set; } = ConsoleColor.White;



        public static void Init(Size size, string title)
        {
            SetSize(size);
            Title = title;
            IsSkipping = false;
            CanSkip = true;
            Console.Clear();
            Console.CursorVisible = false;
            Fill(Frame, ' ');
        }

        public static void SetSize(Size size)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.SetWindowSize(size.Width, size.Height);
                Console.SetBufferSize(size.Width, size.Height);
            }
            Size = size;
            Frame = new Rectangle(0, 0, size.Width, size.Height);
        }

        public static void Clear()
        {
            Clear(Frame);
        }

        public static void Clear(Rectangle frame)
        {
            Fill(frame, ' ');
        }

        public static void Fill(Rectangle frame, char content, ColorOptions options = null)
        {
            if (options == null)
                options = new ColorOptions();
            SetColor(options);

            if (char.IsWhiteSpace(content))
                content = ' ';

            for (int y = frame.Top; y < frame.Bottom; y++)
            {
                Console.SetCursorPosition(frame.X, y);
                for (int x = frame.Left; x < frame.Right; x++)
                { 
                    Console.Write(content);
                }
            }
        }

        public static void Write(Rectangle frame, string text, WriteOptions options = null)
        {
            if (options == null)
                options = new WriteOptions();
            SetColor(options);
            Console.SetCursorPosition(frame.X, frame.Y);
            Console.CursorVisible = options.showCursor;

            var n = 0;
            TextScript next = null;
            if (!options.ignoreScript)
                next = TextScript.GetNext(text);

            for (int y = frame.Top; y < frame.Bottom; y++)
            {
                while (n < text.Length && text[n] == ' ')
                    n++;
                Console.SetCursorPosition(frame.Left, y);
                for (int x = frame.Left; x < frame.Right; x++)
                {
                    if (n < text.Length)
                    {
                        if (!options.ignoreScript)
                        {
                            while (next != null && n == next.Index)
                            {
                                var result = EvaluateScript(next, options);
                                text = text.Remove(n, next.Length);
                                if (result != null)
                                    text = text.Insert(n, result);
                                next = TextScript.GetNext(text, n);
                                if (n >= text.Length)
                                    return;
                            }
                        }

                        if (text[n] == '\n')
                        {
                            Console.Write(' ');
                            if (x >= frame.Right - 1)
                                n++;
                        }
                        else if (text.IndexOf(' ', n) - n > frame.Right - x)
                        {
                            Console.Write(' ');
                        }
                        else
                        {
                            Console.Write(text[n++]);
                            Wait(options.speed);
                        }
                    }
                    else if (options.fill)
                    {
                        Console.Write(' ');
                    }
                    else
                    {
                        return;
                    }
                }
            }

            Console.CursorVisible = false;
        }

        public static void DrawBox(Rectangle frame, BoxOptions options = null)
        {
            if (options == null)
                options = new BoxOptions();
            SetColor(options);

            var outline = options.outline;

            Console.SetCursorPosition(frame.Left, frame.Top);

            Console.Write(outline.TopLeft);

            for (int x = frame.Left + 1; x < frame.Right - 1; x++)
                Console.Write(outline.Horizontal);

            Console.Write(outline.TopRight);

            if (options.fill)
            {
                for (int y = frame.Top + 1; y < frame.Bottom - 1; y++)
                {
                    Console.SetCursorPosition(frame.Left, y);
                    for (int x = frame.Left; x < frame.Right; x++)
                    {
                        if (x == frame.Left || x == frame.Right - 1)
                            Console.Write(outline.Vertical);
                        else
                            Console.Write(' ');
                    }
                }
            }
            else
            {
                for (int y = frame.Top + 1; y < frame.Bottom - 1; y++)
                {
                    Console.SetCursorPosition(frame.Left, y);
                    Console.Write(outline.Vertical);
                    Console.SetCursorPosition(frame.Right - 1, y);
                    Console.Write(outline.Vertical);
                }
            }

            Console.SetCursorPosition(frame.Left, frame.Bottom - 1);

            Console.Write(outline.BottomLeft);

            for (int x = frame.Left + 1; x < frame.Right - 1; x++)
                Console.Write(outline.Horizontal);

            Console.Write(outline.BottomRight);
        }
    
        public static void Wait(double seconds, bool resetSkip = false)
        {
            if (resetSkip)
                IsSkipping = false;

            if (IsSkipping)
                return;

            var now = DateTime.Now;
            var millis = (int)(seconds * 1000);
            while ((DateTime.Now - now).TotalMilliseconds < millis)
            {
                Thread.Sleep(10);
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    IsSkipping = true;
                    return;
                }
            }
        }

        static void Wait(Speed speed)
        {
            switch (speed)
            {
                case Speed.Quick:
                    Wait(0.015);
                    break;
                case Speed.Normal:
                    Wait(0.075);
                    break;
                case Speed.Slow:
                    Wait(0.3);
                    break;
            }
        }

        static void SetColor(ColorOptions options)
        {
            Console.ForegroundColor = options.foreground;
            Console.BackgroundColor = options.background;
        }


        private static string EvaluateScript(TextScript script, WriteOptions options)
        {
            var args = script.Arguments;
            var inst = script.Instruction;
            var count = 0;
            if (args != null)
                count = args.Length;

            switch (script.Instruction)
            {
                case "i":
                case "inst":
                    options.speed = Speed.Instant;
                    break;

                case "q":
                case "quick":
                    options.speed = Speed.Quick;
                    break;

                case "n":
                case "norm":
                    options.speed = Speed.Normal;
                    break;

                case "s":
                case "slow":
                    options.speed = Speed.Slow;
                    break;

                case "w":
                case "wait":
                    double delay;
                    if (count >= 1)
                        if (double.TryParse(args[0], out delay))
                            Wait(delay);
                    break;

                case "f":
                case "fore":
                    ConsoleColor foregroundColor;
                    if (count >= 1)
                        if (Enum.TryParse(args[0], true, out foregroundColor))
                            Console.ForegroundColor = foregroundColor;
                    break;

                case "b":
                case "back":
                    ConsoleColor backgroundColor;
                    if (count >= 1)
                        if (Enum.TryParse(args[0], true, out backgroundColor))
                            Console.ForegroundColor = backgroundColor;
                    break;

                case "p":
                case "parm":
                    if (count >= 1)
                        int.TryParse(args[0], out options.iteration);
                    break;

                case "r":
                case "rand":
                    if (count >= 1)
                        return args.RandomElement();
                    break;

                case "c":
                case "cycl":
                    if (count >= 1)
                        return args[options.iteration % args.Length];
                    break;

                case "a":
                case "augm":
                    if (count >= 1)
                    {
                        if (options.iteration >= args.Length)
                            return args.Last();
                        else
                            return args[options.iteration];
                    }
                    break;

                case "#":
                case "evnt":
                    if (count >= 1)
                        OnScriptEvent(args[0]);
                    break;
            }

            return null;

        }

        public static T RandomElement<T>(this IEnumerable<T> instance)
        {
            return instance.ElementAt(Random.Next(0, instance.Count()));
        }




        static void OnScriptEvent(string argument) => ScriptEvent?.Invoke(argument);
        public static event ScriptEventEventHandler ScriptEvent;
    }

    public delegate void ScriptEventEventHandler(string argument);
}
