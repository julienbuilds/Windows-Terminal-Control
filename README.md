# wctl

Fast commands for controlling Windows. You type `w` and what you want.

```
w spotify              open an app by name
w move firefox left    snap a window to the left half of its monitor
w vol +5               speaker volume up
w mute                 toggle mute
w audio headphones     switch the audio output
w hdr                  toggle HDR
w awake 90m            keep the PC awake for 90 minutes
```

Keyboard shortcuts without having to memorize keyboard shortcuts.

## Install

Download `w.exe` from the [latest release](../../releases/latest) and put it in a folder that is on your `PATH`.
One file, no runtime to install. Windows 10 or 11, 64 bit.

## Commands

Every command is listed in [COMMANDS.md](COMMANDS.md). The same list lives in the terminal:

```
w help
w help move
```

| Area | Commands |
|---|---|
| Apps | `open`, `focus`, `close`, `kill`, `apps` |
| Files | `folder`, `reveal` |
| Windows | `ls`, `move`, `center`, `max`, `min`, `top`, `monitors` |
| Audio | `vol`, `mute`, `mic`, `audio` |
| Display | `hdr`, `brightness` |
| System | `lock`, `sleep`, `awake` |

Rules that apply everywhere:

- Every command has a short alias. `w volume +5`, `w vol +5` and `w v +5` do the same thing.
- Settings that are on or off toggle when you give no argument: `w hdr`, `w mute`, `w mic`, `w top firefox`.
- Windows are picked by their number from `w ls`, by app name, or by part of the title: `w move 2 left`, `w close spotify`, `w focus youtube`.
- Add `--json` to any command to get machine readable output for scripts.
- Exit code 0 means done, 1 means the command could not do what you asked, 2 means an unexpected error. Run again with `--debug` for details.

## Good to know

- `w close` by app name closes all windows of that app. By title or number it closes one window. `w kill` never asks.
- `w hdr` acts on every display that supports HDR.
- `w brightness` talks to the monitor over DDC/CI. Some monitors need that switched on in their menu, and some drop a request now and then. The command retries and tells you when a monitor did not follow. `w brightness 50 monitor 2` sets one monitor.
- `w awake` blocks the terminal and shows a countdown. Ctrl+C ends it early.

## Build from source

Needs the .NET 9 SDK and the Visual Studio C++ build tools (used for the native build).

```
dotnet test
dotnet publish src/Wctl/Wctl.csproj -c Release -r win-x64 -o publish
```

The result is `publish/w.exe`. After adding or changing a command, run `scripts/update-commands.ps1` to regenerate COMMANDS.md. A test checks that the file matches the code.

## Coming next

Scenes (`w scene work` runs a list of commands), a scene editor in the terminal, night light, undo.

## License

[MIT](LICENSE)
