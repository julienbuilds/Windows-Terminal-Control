# Windows Terminal Control commands

Generated from the command table in the code. Do not edit by hand. Regenerate with `scripts/update-commands.ps1`.

Usage: `w <command> [arguments]`. `w <app>` opens the app.
Every command has a long form and short aliases. On/off settings toggle when given no argument.
Add `--json` to any command for machine readable output.
Exit codes: 0 done, 1 the command could not do what was asked, 2 unexpected error (run again with `--debug`).

## Apps

| Command | Aliases | Description |
|---|---|---|
| `w open <app \| file \| folder \| link> [arguments]` | `o` | Open an app, file, folder or link |
| `w focus <window>` | `f` | Bring an app's window to the front |
| `w close <window>` | `x` | Close an app, like clicking its X |
| `w kill <app>` |  | Force an app to quit |
| `w apps [<filter>] [--refresh]` |  | List installed apps, optionally filtered |

## Files

| Command | Aliases | Description |
|---|---|---|
| `w folder [<path>]` |  | Open a folder in Explorer, the current one by default |
| `w reveal <file>` | `select` | Show a file in Explorer with the file selected |

## Windows

| Command | Aliases | Description |
|---|---|---|
| `w ls` | `windows` | List open windows with their numbers |
| `w move <window> left \| right \| <monitor> \| <x> <y> <width> <height>` | `m`, `snap` | Snap a window to a side, move it to a monitor, or place it exactly |
| `w center <window>` |  | Center a window on its monitor |
| `w max <window> [off]` | `maximize` | Maximize a window |
| `w min <window> [off]` | `minimize` | Minimize a window |
| `w top <window> [on \| off]` |  | Keep a window always on top |
| `w monitors` |  | List monitors with their numbers |

## Audio

| Command | Aliases | Description |
|---|---|---|
| `w vol [<n> \| +<n> \| -<n> \| mute \| unmute]` | `volume`, `v` | Show or set the speaker volume |
| `w mute [on \| off]` |  | Mute or unmute the speakers |
| `w mic [mute \| unmute]` |  | Mute or unmute the microphone |
| `w audio [<device>]` | `a` | List audio outputs, or switch to one |

## Display

| Command | Aliases | Description |
|---|---|---|
| `w hdr [on \| off \| status] [monitor <m>]` | `h` | Turn HDR on or off |
| `w brightness [<n> \| +<n> \| -<n>] [monitor <m>]` | `bright` | Show or set monitor brightness |

## System

| Command | Aliases | Description |
|---|---|---|
| `w lock` |  | Lock the screen |
| `w sleep` |  | Put the PC to sleep |
| `w awake [<duration>]` |  | Keep the PC and screen awake for a while |
| `w wait <duration>` |  | Wait for a while |

## Scenes

| Command | Aliases | Description |
|---|---|---|
| `w scene <name> \| list \| show <name> \| new <name> [empty] \| edit <name> \| delete <name> \| file` | `s`, `scenes` | Run, create or edit a scene: a saved list of commands |

## Help

| Command | Aliases | Description |
|---|---|---|
| `w help [<command>]` |  | Show all commands, or details for one |
| `w version` |  | Show the version |
| `w completion powershell` |  | Print the tab completion script for your shell |
| `w complete <word> ... [--end]` |  | Print completion candidates for a half typed command line |
