namespace ngc.Cli;

internal static class NgcCliParser
{
    public static NgcAction Parse(string[] args)
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
        {
            return new HelpAction();
        }

        string command = args[0].ToLowerInvariant();
        return command switch
        {
            "-n" => args.Length > 1 ? new EditAction(args[1]) : new MissingArgumentAction("<domain>"),
            "run" => new RunAction(),
            "fullrun" => new FullRunAction(),
            "rm" => args.Length > 1 ? new RemoveAction(args[1]) : new MissingArgumentAction("<domain>"),
            "list" => new ListAction(),
            "listbak" => new ListBakAction(),
            "restore" => args.Length > 1 ? new RestoreAction(args[1]) : new MissingArgumentAction("<domain>"),
            "ren" => args.Length > 2 ? new RenameAction(args[1], args[2]) : new MissingArgumentAction("<old> <new>"),
            "enable" => args.Length > 1 ? new EnableAction(args[1]) : new MissingArgumentAction("<domain>"),
            "disable" => args.Length > 1 ? new DisableAction(args[1]) : new MissingArgumentAction("<domain>"),
            "clean" => new CleanAction(),
            "ls" => args.Length > 1 ? new ListFolderAction(args[1]) : new MissingArgumentAction("av|en"),
            "set" => args.Length > 2 ? new SetPathAction(args[1], args[2]) : new MissingArgumentAction("av|en <path>"),
            "test" => new TestAction(),
            "version" or "-v" or "--version" => new VersionAction(),
            _ => new UnknownCommandAction(command)
        };
    }
}