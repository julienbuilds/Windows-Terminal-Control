# Commands

Generated from the command table in the code. Do not edit by hand. Regenerate with `scripts/update-commands.ps1`.

Usage: `w <command> [arguments]`. `w <app>` opens the app.
Every command has a long form and short aliases. On/off settings toggle when given no argument.
Add `--json` to any command for machine readable output.
Exit codes: 0 done, 1 the command could not do what was asked, 2 unexpected error (run again with `--debug`).

## Apps

| Command | Aliases | Description |
|---|---|---|
| `w focus <window>` | `f` | Bring an app's window to the front |
| `w close <window>` | `x` | Close an app, like clicking its X |
| `w kill <app>` |  | Force an app to quit |

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

## Help

| Command | Aliases | Description |
|---|---|---|
| `w help [<command>]` |  | Show all commands, or details for one |
| `w version` |  | Show the wctl version |
