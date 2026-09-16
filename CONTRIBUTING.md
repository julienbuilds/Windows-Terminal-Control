# Contributing

Thanks for looking. Bug reports, ideas and pull requests are all welcome.

## Before you open a pull request

Please open an issue first for anything bigger than a fix. It saves you work if the idea does not fit the scope.

The scope test for a new command is the one this project was designed around:

> Would someone use this even if they know nothing about PowerShell or Windows internals?

If the honest answer is "it is handy for developers or admins", it probably belongs in a script rather than in here.

## Getting set up

You need the [.NET 9 SDK](https://dotnet.microsoft.com/download) and the Visual Studio C++ build tools, which the native build uses for linking.

```
git clone https://github.com/julienbuilds/Windows-Terminal-Control.git
cd Windows-Terminal-Control
dotnet test
```

`scripts/install-local.ps1` builds `w.exe` and puts it on your PATH so you can try your change in a real terminal.

## How the code is laid out

| Folder | What lives there |
|---|---|
| `src/Wctl/Cli` | Argument parsing, the command table, the router, output and completion |
| `src/Wctl/Commands` | One file per command, grouped by area |
| `src/Wctl/Platform` | Interfaces for everything that touches Windows |
| `src/Wctl/Platform/Windows` | The real implementations: Win32, COM, DDC/CI |
| `tests/Wctl.Tests` | Tests, with fakes for every platform interface |

## Adding a command

1. Create the command in the right folder under `src/Wctl/Commands`. Copy a small one like `MuteCommand` to see the shape.
2. Register it in `src/Wctl/Commands/Registry.cs`.
3. If it needs something new from Windows, add it to the matching interface in `src/Wctl/Platform`, write the real version under `Platform/Windows`, and add it to the fake in `tests/Wctl.Tests/Fakes`.
4. Add a `Complete` delegate so tab completion knows what can follow.
5. Write tests against the fakes. Every command needs them.
6. Run `scripts/update-commands.ps1` to regenerate `COMMANDS.md`. A test fails if you forget.
7. Try it for real in a terminal.

## House rules

- **Fail loudly.** If something did not work, say so and exit non zero. Never pretend.
- **Every command has tests.** A command that is not tested does not ship.
- **One source of truth for docs.** `COMMANDS.md` and `w help` are both generated from the command table. Never edit `COMMANDS.md` by hand.
- **Plain language.** Error messages are read by people in a hurry. Say what went wrong and what to do about it.
- **No em dashes** anywhere, in code, comments or docs.

## Commit messages

Short, lower case, imperative, and about the change rather than the author.

```
add per app volume
fix volume rounding at 100
```

Not `I added ...` or `Added a thing because ...`.

## Branches and releases

Work happens on a short branch off `main` (`feat/scenes`, `fix/volume-rounding`) and lands through a pull request so CI runs first. `main` is always in a working state.

A release is a tag on `main`. Pushing `v0.2.0` builds `w.exe` and attaches it to a GitHub Release. Release notes are generated from the merged pull requests, so a clear pull request title is the changelog entry.
