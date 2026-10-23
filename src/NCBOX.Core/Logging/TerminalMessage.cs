using System.Collections.Generic;
using System.Linq;

namespace NCBOX.Core.Logging;

public enum TerminalMessageKind
{
    Plain,
    Info,
    Success,
    Warning,
    Error
}

public sealed record TerminalMessage(string Text, TerminalMessageKind Kind = TerminalMessageKind.Plain)
{
    public IReadOnlyList<TerminalSegment>? Segments { get; init; }

    public static TerminalMessage FromSegments(params TerminalSegment[] segments) =>
        new(string.Concat(segments.Select(segment => segment.Text))) { Segments = segments };
}
