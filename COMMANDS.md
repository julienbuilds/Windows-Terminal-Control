# Commands

Generated from the command table in the code. Do not edit by hand. Regenerate with `scripts/update-commands.ps1`.

Usage: `w <command> [arguments]`. `w <app>` opens the app.
Every command has a long form and short aliases. On/off settings toggle when given no argument.
Add `--json` to any command for machine readable output.
Exit codes: 0 done, 1 the command could not do what was asked, 2 unexpected error (run again with `--debug`).

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
