namespace cbc.Cli;

internal static class CbcCliParser
{
    public static CbcAction Parse(string[] args)
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h") return new HelpAction();
        string command = args[0].ToLowerInvariant();
        return command switch
        {
            "list" => new ListAction(),
            "inspect" => args.Length > 1 ? new InspectAction(args[1]) : new MissingArgumentAction("<domain>"),
            "create" => args.Length > 1 ? new CreateAction(args[1..]) : new MissingArgumentAction("<domain...>"),
            "merge" => args.Length > 1 ? new MergeAction(args[1..]) : new MissingArgumentAction("<domains...>"),
            "renew" => new RenewAction(),
            "autorenew" => new AutoRenewAction(),
            "check" => new CheckAction(),
            "remove" or "rm" => args.Length > 1 ? new RemoveAction(args[1]) : new MissingArgumentAction("<domain>"),
            "version" or "-v" or "--version" => new VersionAction(),
            _ => new UnknownCommandAction(command)
        };
    }
}