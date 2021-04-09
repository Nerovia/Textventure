using System;
using System.Collections.Generic;
using System.Text;

namespace Textventure.Engine
{
    public class BoxOutline
    {
        public BoxOutline(char horizontal, char vertical, char topLeft, char topRight, char bottomLeft, char bottomRight)
        {
            Horizontal = horizontal;
            Vertical = vertical;
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }

        public char Horizontal { get; }
        public char Vertical { get; }
        public char TopLeft { get; }
        public char TopRight { get; }
        public char BottomLeft { get; }
        public char BottomRight { get; }

        public static readonly BoxOutline Single = new BoxOutline('─', '│', '┌', '┐', '└', '┘');
        public static readonly BoxOutline Double = new BoxOutline('═', '║', '╔', '╗', '╚', '╝');
        public static readonly BoxOutline None = new BoxOutline(' ', ' ', ' ', ' ', ' ', ' ');
    }
}
