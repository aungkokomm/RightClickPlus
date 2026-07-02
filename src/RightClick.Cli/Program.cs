namespace RightClick.Cli;

// RightClick+ helper. Invoked by the context-menu verbs, once per selected file.
// Usage:
//   RightClick.Cli file-to-folder "<path>"
//   RightClick.Cli file-note      "<path>"
//   RightClick.Cli copy <name|name-noext|path|path-quoted> "<path>"
internal static class Program
{
    [STAThread] // required for the WinForms clipboard
    private static int Main(string[] args)
    {
        try
        {
            // Cleanup verb (no path argument) — run by the uninstaller before files are removed,
            // so no orphaned right-click entries remain. Handled before the two-arg guard below.
            if (args.Length >= 1 && string.Equals(args[0], "unregister-all", StringComparison.OrdinalIgnoreCase))
            {
                Commands.UnregisterAll();
                return 0;
            }

            if (args.Length < 2)
            {
                return 0;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "file-to-folder":
                    Commands.FileToFolder(args[1]);
                    break;

                case "file-note":
                    if (SelectionAggregator.TryBecomeLeader("note", args[1], out var collectNote))
                    {
                        Commands.FileNote(collectNote());
                    }
                    break;

                case "copy":
                    if (args.Length < 3)
                    {
                        return 0;
                    }
                    var variant = args[1].ToLowerInvariant();
                    if (SelectionAggregator.TryBecomeLeader($"copy.{variant}", args[2], out var collectCopy))
                    {
                        Commands.Copy(variant, collectCopy());
                    }
                    break;

                case "folder-color":
                    if (args.Length >= 3)
                    {
                        Commands.FolderColor(args[1], args[2]);
                    }
                    break;

                case "register":
                    Commands.Register(args[1], install: true);
                    break;

                case "unregister":
                    Commands.Register(args[1], install: false);
                    break;
            }
        }
        catch (Exception ex)
        {
            CliLog.Error("Unhandled exception in CLI.", ex);
            return 1;
        }

        return 0;
    }
}
