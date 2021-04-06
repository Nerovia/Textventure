using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Textventure.Engine
{
    public class TextScript
    {
        private TextScript(int index, int length, string instruction, string[] arguments)
        {
            Index = index;
            Length = length;
            Instruction = instruction;
            Arguments = arguments;
        }

        public static TextScript GetNext(string text, int start = 0)
        {
            var index = 0;
            var length = 0;
            var instruction = "";
            var args = new List<string>();

            string buffer = "";
            int depth = 0;
            int state = 0;

            for (int n = start; n < text.Length; n++)
            {
                var c = text[n];
                switch (state)
                {
                    // Look for instruction start '['
                    case 0:
                        if (c == '[')
                        {
                            state = 1;
                            index = n;
                        }
                        break;

                    // Look for instruction end ']'
                    case 1:
                        if (c == ']')
                        {
                            state = 2;
                            instruction = buffer;
                            length = n - index + 1;
                        }
                        else if (c == '[')
                        {
                            return new TextScript(n, 1, null, null);
                        }
                        else
                        {
                            buffer += c;
                        }
                        break;

                    // Check for arguments
                    case 2:
                        if (c == '{')
                        {
                            depth++;
                            buffer = "";
                            state = 3;
                        }
                        else
                        {
                            return new TextScript(index, length, instruction, args.ToArray());
                        }
                        break;

                    // Get argument
                    case 3:
                        if (c == '{')
                        {
                            depth++;
                        }
                        else if (c == '}')
                        {
                            depth--;
                            if (depth == 0)
                            {
                                length = n - index + 1;
                                args.Add(buffer);
                                state = 2;
                                break;
                            }
                        }
                        buffer += c;
                        break;
                }
            }

            if (state < 2)
                return null;
            return new TextScript(index, length, instruction, args.ToArray());
        }

        public int Length { get; }
        public int Index { get; }
        public string[] Arguments { get; }
        public string Instruction { get; }
    }

}
