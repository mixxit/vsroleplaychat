using System;
using System.Drawing;

namespace vsroleplaychat.src
{
    internal class HexColor
    {
        public static string ColorMessage(Color color, string message)
        {
            return String.Format("<font color=\"{0}\">{1}</font>", ToHex(color), message);
        }

        public static string ColorMessage(string hex, string message)
        {
            return String.Format("<font color=\"{0}\">{1}</font>", hex, message);
        }

        public static string ToHex(Color color)
        {
            var hexNumber = color.R;
            var colorR = hexNumber.ToString("X2");
            hexNumber = color.G;
            string colorG = hexNumber.ToString("X2");
            hexNumber = color.B;
            string colorB = hexNumber.ToString("X2");
            return String.Format("#{0}{1}{2}", colorR, colorG, colorB);
        }
    }
}