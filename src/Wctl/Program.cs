using System.Text;
using Spectre.Console;
using Wctl.Cli;
using Wctl.Commands;
using Wctl.Platform;

Console.OutputEncoding = Encoding.UTF8;

var errorConsole = AnsiConsole.Create(new AnsiConsoleSettings
{
    Out = new AnsiConsoleOutput(Console.Error),
});

return App.Run(args, Registry.Build(), Services.Real(), AnsiConsole.Console, errorConsole, Console.Out);
