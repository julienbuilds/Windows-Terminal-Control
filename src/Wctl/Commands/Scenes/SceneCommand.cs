using System.Globalization;
using Wctl.Cli;
using Wctl.Platform;
using Wctl.Scenes;

namespace Wctl.Commands.Scenes;

public static class SceneCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "scene",
        Aliases = ["s", "scenes"],
        Group = Groups.Scenes,
        Summary = "Run, create or edit a scene: a saved list of commands",
        Usage = "scene <name> | list | show <name> | new <name> [empty] | edit <name> | delete <name> | file",
        Details = "A scene runs its steps in order and reports each one; a failing step does not stop the rest. "
            + "'w work' also runs the scene named work. 'w scene new work' captures your current setup (sound, displays, "
            + "open apps and their windows) into steps you can review, trim and save; add 'empty' to start blank. "
            + "Scenes live in one text file, one command per line under a [name] header; 'w scene file' opens it.",
        MaxArgs = 3,
        Complete = Complete,
        Run = Run,
    };

    private static readonly string[] Subcommands = ["list", "show", "new", "edit", "delete", "file"];

    private static IEnumerable<string> Complete(CompletionContext ctx) => ctx.Position switch
    {
        0 => Subcommands.Concat(ctx.SceneNames()),
        1 when ctx.Word(0) is "show" or "edit" or "delete" => ctx.SceneNames(),
        1 when ctx.Word(0) is "new" => [],
        2 when ctx.Word(0) is "new" => ["empty"],
        _ => [],
    };

    private static int Run(Invocation inv)
    {
        var store = inv.Services.Scenes;
        switch (inv.FirstArg)
        {
            case null or "list":
                return List(Load(store), inv.Output);

            case "show":
                return Show(Find(Load(store), Name(inv)), inv.Output);

            case "new":
                return New(inv, Name(inv));

            case "edit":
                var scene = Find(Load(store), Name(inv));
                return new SceneEditor(inv).Run(scene.Name, scene.Steps.ToList());

            case "delete":
                return Delete(store, Name(inv), inv.Output);

            case "file":
                return OpenFile(store, inv.Services.Shell, inv.Output);

            default:
                if (inv.Args.Count > 1)
                {
                    throw new WctlException($"Too many arguments. Usage: w {Spec.Usage}");
                }

                return SceneRunner.Run(Find(Load(store), inv.Args[0]), inv);
        }
    }

    public static List<Scene> Load(ITextStore store) => SceneFile.Parse(store.Read() ?? string.Empty);

    public static Scene Find(List<Scene> scenes, string name)
        => scenes.Find(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? throw new WctlException($"No scene named '{name}'. Run 'w scene list' to see them.");

    private static string Name(Invocation inv)
        => inv.Args.Count > 1 ? inv.Args[1] : throw new WctlException($"Which scene? Usage: w {Spec.Usage}");

    private static int New(Invocation inv, string name)
    {
        SceneFile.ValidateName(name);
        if (inv.Table.Find(name) is not null)
        {
            throw new WctlException($"'{name}' is a command name. Pick another name for the scene.");
        }

        if (Load(inv.Services.Scenes).Exists(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new WctlException($"A scene named '{name}' exists. Use 'w scene edit {name}' or 'w scene delete {name}'.");
        }

        var empty = inv.Args.Count > 2 && inv.Args[2].Equals("empty", StringComparison.OrdinalIgnoreCase);
        if (inv.Args.Count > 2 && !empty)
        {
            throw new WctlException($"Expected 'empty' or nothing after the name. Got '{inv.Args[2]}'. Usage: w {Spec.Usage}");
        }

        var app = inv.AppCatalog.Apps.FirstOrDefault(a =>
            a.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || a.Executable.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (app is not null)
        {
            inv.Output.Message($"Note: '{name}' is also the app {app.Name}. 'w {name}' will run the scene; 'w open {name}' still opens the app.");
        }

        var steps = empty ? [] : SceneCapture.Capture(inv);
        return new SceneEditor(inv).Run(name, steps);
    }

    private static int List(List<Scene> scenes, IOutput output)
    {
        if (scenes.Count == 0)
        {
            output.Message("No scenes yet. Create one with 'w scene new <name>'.");
        }

        var rows = scenes.Select(s => new[] { s.Name, s.Steps.Count.ToString(CultureInfo.InvariantCulture) }).ToList();
        output.Table("scenes", ["name", "steps"], rows);
        return ExitCodes.Ok;
    }

    private static int Show(Scene scene, IOutput output)
    {
        output.State("Scene", scene.Name);
        if (scene.Steps.Count == 0)
        {
            output.Message("No steps.");
        }

        var rows = scene.Steps.Select(step => new[] { step }).ToList();
        output.Table("steps", ["step"], rows, new TableOptions(Header: false));
        return ExitCodes.Ok;
    }

    private static int Delete(ITextStore store, string name, IOutput output)
    {
        var scenes = Load(store);
        var scene = Find(scenes, name);
        scenes.Remove(scene);
        store.Write(SceneFile.Format(scenes));
        output.State("Deleted", scene.Name);
        return ExitCodes.Ok;
    }

    private static int OpenFile(ITextStore store, IShell shell, IOutput output)
    {
        if (store.Read() is null)
        {
            store.Write(SceneFile.Format([]));
        }

        shell.Open(store.FilePath);
        output.State("Scenes file", store.FilePath);
        return ExitCodes.Ok;
    }
}
