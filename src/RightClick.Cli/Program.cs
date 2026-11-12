namespace RightClick.Cli;

// RightClick+ helper. Explorer starts it as a COM server ("-Embedding") when a menu entry is clicked
// and hands over the whole selection in one call (see ComServer). It can also be run directly:
//   RightClick.Cli file-to-folder <path...>
//   RightClick.Cli file-note      <path>
//   RightClick.Cli copy <name|name-noext|path|path-quoted> <path...>
//   RightClick.Cli folder-color <color|reset> <path...>
//   RightClick.Cli register|unregister <action>
//   RightClick.Cli refresh-verbs     (run by the installer)
//   RightClick.Cli unregister-all    (run by the uninstaller)
internal static class Program
{
    [STAThread] // required for serving COM and for the WinForms clipboard
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                return 0;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "-embedding" or "/embedding":
                    ComServer.Run();
                    break;

                case "refresh-verbs":
                    Commands.RefreshVerbs();
                    break;

                case "unregister-all":
                    Commands.UnregisterAll();
                    break;

                case "register" when args.Length >= 2:
                    Commands.Register(args[1], install: true);
                    break;

                case "unregister" when args.Length >= 2:
                    Commands.Register(args[1], install: false);
                    break;

                case "copy" or "folder-color" when args.Length >= 3:
                    Commands.Run($"{args[0]} {args[1]}", args[2..]);
                    break;

                case "file-to-folder" or "file-note" when args.Length >= 2:
                    Commands.Run(args[0], args[1..]);
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
