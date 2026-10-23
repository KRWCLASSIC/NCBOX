namespace cbc.Cli;

internal abstract record CbcAction;
internal sealed record HelpAction : CbcAction;
internal sealed record VersionAction : CbcAction;
internal sealed record ListAction : CbcAction;
internal sealed record InspectAction(string Domain) : CbcAction;
internal sealed record CreateAction(string[] Domains) : CbcAction;
internal sealed record MergeAction(string[] Domains) : CbcAction;
internal sealed record RenewAction : CbcAction;
internal sealed record AutoRenewAction : CbcAction;
internal sealed record CheckAction : CbcAction;
internal sealed record RemoveAction(string Domain) : CbcAction;
internal sealed record MissingArgumentAction(string Expected) : CbcAction;
internal sealed record UnknownCommandAction(string Command) : CbcAction;