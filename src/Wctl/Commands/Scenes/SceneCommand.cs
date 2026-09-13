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
        Summary = "Run a scene: a saved list of commands",
        Usage = "scene <name> | list | show <name> | delete <name> | file",
        Details = "A scene runs its steps in order and reports each one; a failing step does not stop the rest. "
            + "'w work' also runs the scene named work. Scenes live in one text file, one command per line under a [name] header; "
            + "'w scene file' opens it. Put 'wait 2s' between opening an app and moving its window.",
        MaxArgs = 2,
        Run = Run,
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

    public static List<Scene> Load(ISceneStore store) => SceneFile.Parse(store.Read() ?? string.Empty);

    public static Scene Find(List<Scene> scenes, string name)
        => scenes.Find(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? throw new WctlException($"No scene named '{name}'. Run 'w scene list' to see them.");

    private static string Name(Invocation inv)
        => inv.Args.Count > 1 ? inv.Args[1] : throw new WctlException($"Which scene? Usage: w {Spec.Usage}");

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

    private static int Delete(ISceneStore store, string name, IOutput output)
    {
        var scenes = Load(store);
        var scene = Find(scenes, name);
        scenes.Remove(scene);
        store.Write(SceneFile.Format(scenes));
        output.State("Deleted", scene.Name);
        return ExitCodes.Ok;
    }

    private static int OpenFile(ISceneStore store, IShell shell, IOutput output)
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
