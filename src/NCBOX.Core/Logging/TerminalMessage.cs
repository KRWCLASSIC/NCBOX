namespace NCBOX.Core.Logging;

public enum TerminalMessageKind
{
    Plain,
    Info,
    Success,
    Warning,
    Error
}

public sealed record TerminalMessage(string Text, TerminalMessageKind Kind = TerminalMessageKind.Plain);