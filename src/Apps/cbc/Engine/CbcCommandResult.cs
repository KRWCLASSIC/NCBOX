using System.Collections.Generic;
using NCBOX.Core.Logging;

namespace cbc.Engine;

internal sealed record CbcCommandResult(int ExitCode, IReadOnlyList<TerminalMessage> Messages)
{
    public static CbcCommandResult Empty(int exitCode = 0) => new(exitCode, []);
    public static CbcCommandResult One(int exitCode, string text, TerminalMessageKind kind = TerminalMessageKind.Plain) =>
        new(exitCode, [new TerminalMessage(text, kind)]);
}