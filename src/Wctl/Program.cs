using System.Text;
using Spectre.Console;
using Wctl.Cli;
using Wctl.Commands;

Console.OutputEncoding = Encoding.UTF8;

var errorConsole = AnsiConsole.Create(new AnsiConsoleSettings
{
    Out = new AnsiConsoleOutput(Console.Error),
});

return App.Run(args, Registry.Build(), AnsiConsole.Console, errorConsole, Console.Out);
