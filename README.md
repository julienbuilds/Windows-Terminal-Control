# wctl

Fast commands for controlling Windows. You type `w` and what you want.

```
w vol +5
w mute
w hdr
w move firefox left
w audio headphones
w spotify
```

Keyboard shortcuts without having to memorize keyboard shortcuts.

## Install

Download `w.exe` from the [latest release](../../releases/latest) and put it in a folder that is on your `PATH`.
No runtime to install. Windows 10 or 11, 64 bit.

## Commands

Every command is listed in [COMMANDS.md](COMMANDS.md). The same list is available in the terminal:

```
w help
w help vol
```

Rules that apply everywhere:

- Every command has a long form and a short alias. `w volume +5` and `w v +5` do the same thing.
- On/off settings toggle when you give no argument: `w hdr`, `w mute`, `w mic`.
- Add `--json` to any command to get machine readable output for scripts.
- Exit code 0 means done, 1 means the command could not do what you asked, 2 means an unexpected error.

## Build from source

Needs the .NET 9 SDK and the Visual Studio C++ build tools (used for the native build).

```
dotnet test
dotnet publish src/Wctl/Wctl.csproj -c Release -r win-x64 -o publish
```

The result is `publish/w.exe`.

## License

[MIT](LICENSE)
