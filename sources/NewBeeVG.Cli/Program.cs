using NewBeeVG.Cli.Tools;

namespace NewBeeVG.Cli;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
            PrintHelp();
        var cmd = args[0].Trim();

        args = ShiftArgs(args);

        if(cmd == "watch")
        {
            new WatchTool().Run(args);
        }
        else
        {
            PrintHelp();
        }
    }

    static string[] ShiftArgs(string[] args)
    {
        var list = new List<String>();
        for (int i = 1; i < args.Length; i++)
            list.Add(args[i]);
        return list.ToArray();
    }

    static void PrintHelp()
    {

    }
}
