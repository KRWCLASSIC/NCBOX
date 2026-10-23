using System;
using System.Collections.Generic;
using System.IO;

namespace NCBOX.Core.Logging;

public static class TerminalConsole
{
    private static bool UseColor(TextWriter writer) =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")) &&
        (ReferenceEquals(writer, Console.Error) ? !Console.IsErrorRedirected : !Console.IsOutputRedirected);

    public static void WriteLine(TerminalMessage message)
    {
        IReadOnlyList<TerminalSegment> segments = message.Segments ?? [new TerminalSegment(message.Text, ToStyle(message.Kind))];
        Write(ConsoleErrorFor(message), segments);
    }

    public static void WriteLine(params TerminalSegment[] segments) => Write(Console.Out, segments);

    private static TextWriter ConsoleErrorFor(TerminalMessage message) =>
        message.Kind == TerminalMessageKind.Error ? Console.Error : Console.Out;

    private static void Write(TextWriter writer, IReadOnlyList<TerminalSegment> segments)
    {
        if (!UseColor(writer))
        {
            foreach (TerminalSegment segment in segments) writer.Write(segment.Text);
            writer.WriteLine();
            return;
        }

        ConsoleColor original = Console.ForegroundColor;
        try
        {
            foreach (TerminalSegment segment in segments)
            {
                ConsoleColor? color = ToColor(segment.Style);
                Console.ForegroundColor = color ?? original;
                writer.Write(segment.Text);
            }
            writer.WriteLine();
        }
        finally
        {
            Console.ForegroundColor = original;
        }
    }

    private static TerminalStyle ToStyle(TerminalMessageKind kind) => kind switch
    {
        TerminalMessageKind.Info => TerminalStyle.Cyan,
        TerminalMessageKind.Success => TerminalStyle.Green,
        TerminalMessageKind.Warning => TerminalStyle.Yellow,
        TerminalMessageKind.Error => TerminalStyle.Red,
        _ => TerminalStyle.Plain
    };

    private static ConsoleColor? ToColor(TerminalStyle style) => style switch
    {
        TerminalStyle.Cyan => ConsoleColor.Cyan,
        TerminalStyle.Green => ConsoleColor.Green,
        TerminalStyle.Yellow => ConsoleColor.Yellow,
        TerminalStyle.Red => ConsoleColor.Red,
        TerminalStyle.Magenta => ConsoleColor.Magenta,
        TerminalStyle.BoldGray => ConsoleColor.DarkGray,
        TerminalStyle.White => ConsoleColor.White,
        _ => null
    };
}
