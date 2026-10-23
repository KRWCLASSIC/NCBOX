namespace NCBOX.Core.Logging;

public enum TerminalStyle
{
    Plain,
    Cyan,
    Green,
    Yellow,
    Red,
    Magenta,
    BoldGray,
    White
}

public sealed record TerminalSegment(string Text, TerminalStyle Style = TerminalStyle.Plain);
