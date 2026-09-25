namespace MyPrint
{
    public enum ABColor
    {
        Red,
        Blue,
        Green,
        Yellow,
        Orange,
        Pink,
        Purple,
        Black,
        Grey,
        White
    }

    public enum ConsoleStyle
    {
        Bold,
        Italic,
        Underline,
    }

    public class ABOption
    {
        public static string GetColor(ABColor abColor)
        {
            switch (abColor)
            {
                case ABColor.Red: return "<color=red>";
                case ABColor.Blue: return "<color=blue>";
                case ABColor.Green: return "<color=green>";
                case ABColor.Yellow: return "<color=yellow>";
                case ABColor.Orange: return "<color=orange>";
                case ABColor.Pink: return "<color=#FFC0CB>";
                case ABColor.Purple: return "<color=purple>";
                case ABColor.Black: return "<color=black>";
                case ABColor.Grey: return "<color=grey>";
                case ABColor.White:
                default: return "<color=white>";
            }
        }

        public static (string, string) GetStyle(ConsoleStyle style)
        {
            switch (style)
            {
                case ConsoleStyle.Bold: return ("<b>", "</b>");
                case ConsoleStyle.Italic: return ("<i>", "</i>");
                default: return ("", "");
            }
        }
    }
}
