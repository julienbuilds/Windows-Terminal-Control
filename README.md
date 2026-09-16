<div align="center">

# Windows Terminal Control

### Stop clicking through Windows. Type `w` and what you want.

A tiny command line tool that controls Windows: launch and close apps, move windows between monitors,
change the volume, switch the audio output, toggle HDR, set brightness, lock the PC, and replay a whole
desktop setup with one word.

[![ci](https://github.com/julienbuilds/Windows-Terminal-Control/actions/workflows/ci.yml/badge.svg)](https://github.com/julienbuilds/Windows-Terminal-Control/actions/workflows/ci.yml)
[![release](https://img.shields.io/github/v/release/julienbuilds/Windows-Terminal-Control?display_name=tag&sort=semver&color=0078D4)](https://github.com/julienbuilds/Windows-Terminal-Control/releases/latest)
[![downloads](https://img.shields.io/github/downloads/julienbuilds/Windows-Terminal-Control/total?color=success)](https://github.com/julienbuilds/Windows-Terminal-Control/releases)
[![license](https://img.shields.io/github/license/julienbuilds/Windows-Terminal-Control)](LICENSE)

![windows](https://img.shields.io/badge/Windows-10%20and%2011-0078D4?logo=windows&logoColor=white)
![size](https://img.shields.io/badge/one%20file-4.7%20MB-blue)
![runtime](https://img.shields.io/badge/runtime-none%20needed-success)
![startup](https://img.shields.io/badge/startup-~20%20ms-brightgreen)

</div>

```console
PS C:\> w vol +5
Volume: 47%

PS C:\> w audio headphones
Audio output: Headphones (BTD 600)

PS C:\> w move firefox left
firefox: left half of monitor 1

PS C:\> w hdr monitor main
HDR: on

PS C:\> w spotify
Spotify: opened

PS C:\> w gaming
Scene: gaming
  ✓ hdr on monitor main            HDR: on
  ✓ vol 35                         Volume: 35%
  ✓ close slack                    slack: closing
  ✓ open steam                     Steam: opened
4 steps, all done.
```

Think of it as keyboard shortcuts you do not have to memorise. You already know the words.

## Why

Windows hides everyday settings behind menus, and the things you do ten times a day are the slowest ones.
Turning HDR on for one monitor, moving a window to the other screen, or switching from speakers to headphones
all take a trip through the Settings app or a fiddly flyout.

Every one of those is a short command here, and the commands are the words you would use out loud.

```
w vol 20        w mute          w mic
w hdr           w brightness 60 w monitors
w open steam    w close discord w focus firefox
w move code 2   w max code      w top spotify
w lock          w sleep         w awake 90m
```

There is no daemon, no tray icon, no account and no network. It is one executable that runs, does the thing,
and exits, usually in about 20 milliseconds.

## Install

Download `w.exe` from the [latest release](https://github.com/julienbuilds/Windows-Terminal-Control/releases/latest)
and drop it in any folder on your `PATH`. That is the whole install. No runtime, no installer, no admin rights.

If you would rather build it, see [Build from source](#build-from-source). The script there also puts it on your PATH for you.

### Tab completion

One command, once:

```powershell
w completion install $PROFILE
```

That adds a marked block to your PowerShell profile. It is safe to run again, because it replaces its own block
rather than adding a second one, and it keeps whatever encoding your profile already uses. If you would rather
place the script yourself, `w completion powershell` just prints it.

Open a new terminal, and Tab completes commands, your scenes, installed apps, open windows, audio devices and monitors.

Every suggestion carries a short description. Press `Ctrl+Space` instead of Tab to get the menu that shows them:

```console
PS C:\> w hdr <CTRL+SPACE>
on        turn HDR on
off       turn HDR off
status    only show the state
monitor   pick one monitor

PS C:\> w hdr monitor <CTRL+SPACE>
main      the primary monitor
1         3840x2160, primary
2         2560x1440

PS C:\> w close <CTRL+SPACE>
firefox           Pull requests - Mozilla Firefox
WindowsTerminal   2 windows
1                 firefox: Pull requests - Mozilla Firefox
2                 WindowsTerminal: PowerShell

PS C:\> w audio <CTRL+SPACE>
Speakers (FiiO K11)             current output
Headphones (BTD 600)            audio output
32M2V (NVIDIA High Definition)  audio output
```

The block it writes ends with a commented line that binds Tab to that same menu, if you would rather have it there.

## What it can do

Every command has a long form and a short alias, and settings that are on or off toggle when you give no argument.

| Area | Commands | Example |
|---|---|---|
| **Apps** | `open` `focus` `close` `kill` `apps` | `w spotify` is short for `w open spotify` |
| **Files** | `folder` `reveal` | `w folder` opens the current directory in Explorer |
| **Windows** | `ls` `move` `center` `max` `min` `top` `monitors` | `w move firefox 2` sends it to the second screen |
| **Audio** | `vol` `mute` `mic` `audio` | `w vol +5`, `w mic`, `w audio fiio` |
| **Display** | `hdr` `brightness` | `w hdr monitor main`, `w brightness 60` |
| **System** | `lock` `sleep` `awake` `wait` | `w awake 90m` keeps the PC up with a countdown |
| **Scenes** | `scene` | `w work` replays your whole setup |

The full list with every argument lives in [COMMANDS.md](COMMANDS.md), and `w help` prints the same thing in your terminal.
It is generated from the code, so it cannot drift.

### A few worth knowing about

**Windows are found the way you think about them.** By app name, by part of the title, or by the number from `w ls`.

```console
PS C:\> w ls
╭───┬───────┬─────────────────┬────────────────────────────────────┬─────────╮
│   │ index │ app             │ title                              │ monitor │
├───┼───────┼─────────────────┼────────────────────────────────────┼─────────┤
│ ● │ 1     │ firefox         │ Pull requests - Mozilla Firefox    │ 1       │
│   │ 2     │ Discord         │ Discord                            │ 2       │
│   │ 3     │ WindowsTerminal │ PowerShell                         │ 1       │
╰───┴───────┴─────────────────┴────────────────────────────────────┴─────────╯

PS C:\> w move 2 left
Discord: left half of monitor 2
```

**HDR per monitor.** `w hdr` flips every HDR capable display, `w hdr monitor main` flips only the primary one.

**Brightness over DDC/CI**, the same channel your monitor's own buttons use, so it works on desktop monitors and not
just laptop panels. Some monitors need DDC/CI switched on in their menu, and some drop a request now and then, so the
command retries and tells you plainly when a monitor did not follow.

**The default audio device**, which Windows does not expose to scripts at all. `w audio` lists your outputs and marks
the active one, `w audio fiio` switches to it.

## Scenes

A scene is a saved list of commands. The useful part is that you do not write it by hand:
`w scene new work` looks at how your desktop is set up right now and turns it into steps.

```console
PS C:\> w scene new work

Scene work:
    1  vol 15
    2  mic unmute
    3  audio "Speakers (Focusrite USB Audio)"
    4  hdr off monitor 1
    5  brightness 85 monitor 1
    6  open firefox
    7  open Code
    8  wait 2s
    9  move firefox 1
   10  max firefox
   11  move Code 2

r <n> remove   m <n> <to> move   a <step> add   t <n> test   s save   q quit
> r 2
> s
Saved: work (10 steps)
```

From then on, `w work` replays it. Every step reports itself, and one failing step does not stop the others,
so closing an app that is not running will not ruin the rest of your morning.

Scenes live in one plain text file, one command per line. `w scene file` opens it if you would rather edit it directly.

```ini
[gaming]
hdr on monitor main
vol 35
close slack
open steam
```

## Scripting

Add `--json` to any command and you get one machine readable object instead of a table.

```console
PS C:\> w hdr status --json
{"hdr":false,"displays":[{"monitor":"1","hdr":"off"},{"monitor":"2","hdr":"off"}]}

PS C:\> if ((w hdr status --json | ConvertFrom-Json).hdr -eq $false) { w hdr on }
```

Exit codes are stable: `0` done, `1` the command could not do what you asked, `2` something unexpected.
Add `--debug` to see the full error.

## FAQ

<details>
<summary><b>Is this Windows Terminal?</b></summary>

No. Windows Terminal is Microsoft's terminal application. This is a command you type inside a terminal,
whether that is Windows Terminal, the classic console, or anything else. The name means "controlling Windows,
from a terminal".
</details>

<details>
<summary><b>Why is the command just <code>w</code>?</b></summary>

Because you type it constantly, and `w vol +5` is fast enough that you stop thinking about it.
Nothing else on Windows uses `w`, so there is no collision.
</details>

<details>
<summary><b>Does it need administrator rights?</b></summary>

No. It runs as you, and it never asks to be elevated.
</details>

<details>
<summary><b>Does it run in the background or phone home?</b></summary>

Neither. There is no service, no tray icon, no telemetry and no network code at all. The process starts,
does one job, and exits.
</details>

<details>
<summary><b>Why not just write PowerShell scripts?</b></summary>

You can, and for some of this it is fine. The parts that are genuinely unpleasant in PowerShell are window
management, the default audio device and HDR, which need Win32, COM and the display configuration API rather
than a cmdlet. This wraps all of that behind commands you can remember.
</details>

<details>
<summary><b>Does it work on Windows 10?</b></summary>

It is built for Windows 10 and 11 and uses nothing newer than Windows 10 offers. It has been tested on
Windows 11 only, so if you are on 10 and something misbehaves, please open an issue.
</details>

<details>
<summary><b>What about night light, per app volume, undo?</b></summary>

On the list, not written yet. See [what is next](#what-is-next).
</details>

## Build from source

You need the [.NET 9 SDK](https://dotnet.microsoft.com/download) and the Visual Studio C++ build tools,
which the native build uses for linking.

```powershell
git clone https://github.com/julienbuilds/Windows-Terminal-Control.git
cd Windows-Terminal-Control
dotnet test
.\scripts\install-local.ps1
```

`install-local.ps1` publishes `w.exe`, copies it to `%LOCALAPPDATA%\Programs\wctl` and puts that folder on your PATH.

It is C# on .NET 9, published with Native AOT, so what comes out is a single self contained executable with no
runtime to install and no measurable startup cost. Windows is reached through Win32, the Core Audio COM interfaces,
the display configuration API and DDC/CI, all behind interfaces so the commands can be tested without touching
your actual machine.

## What is next

Undo for the last change, night light, per app volume, and `w restart <app>`.
Ideas are welcome in the [issue tracker](https://github.com/julienbuilds/Windows-Terminal-Control/issues).

## Contributing

Pull requests are welcome. [CONTRIBUTING.md](CONTRIBUTING.md) covers the layout, how to add a command,
and the two house rules that matter: fail loudly, and every command ships with tests.

## License

[MIT](LICENSE)
