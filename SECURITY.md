# Security

## Reporting a problem

Please do not open a public issue for a security problem.

Use [GitHub's private advisory form](https://github.com/julienbuilds/Windows-Terminal-Control/security/advisories/new), or write to julien.schweter@proton.me.

Tell me what you found, how to reproduce it, and what an attacker could do with it. You will get a reply within a week.

## Supported versions

The newest release is the supported one. This is a young project, so fixes go into the next release rather than into older tags.

## What this tool can do on your machine

`w` runs with your own user rights and never asks for administrator rights. It can start and stop programs, move windows, change sound and display settings, and lock or suspend the PC. It does not install a service, does not run in the background, and does not send anything over the network.

It reads and writes two files of its own:

| File | What for |
|---|---|
| `%APPDATA%\wctl\scenes.txt` | Your scenes, in plain text |
| `%LOCALAPPDATA%\wctl\apps.txt` | A cached list of installed app names, rebuilt daily |

Scenes are plain command lines that `w` runs as you. Treat a scenes file from someone else the way you would treat their script.
