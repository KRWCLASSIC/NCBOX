using System.Collections.Generic;
using NCBOX.Core.Logging;

namespace ngc.Engine;

internal sealed record NgcCommandResult(int ExitCode, IReadOnlyList<TerminalMessage> Messages)
{
    public static NgcCommandResult Empty(int exitCode = 0) => new(exitCode, []);
    public static NgcCommandResult One(int exitCode, string text, TerminalMessageKind kind = TerminalMessageKind.Plain) =>
        new(exitCode, [new TerminalMessage(text, kind)]);
}