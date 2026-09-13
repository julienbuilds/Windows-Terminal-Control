# Builds w.exe and puts it in a stable folder on your user PATH, so 'w' works from any terminal.
# Run it again after changes to update the installed copy.

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$target = Join-Path $env:LOCALAPPDATA "Programs\wctl"

dotnet publish (Join-Path $root "src/Wctl/Wctl.csproj") -c Release -r win-x64 -o (Join-Path $root "publish") --nologo | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Publish failed with exit code $LASTEXITCODE" }

New-Item -ItemType Directory -Force $target | Out-Null
Copy-Item (Join-Path $root "publish\w.exe") (Join-Path $target "w.exe") -Force

$entries = @([Environment]::GetEnvironmentVariable("Path", "User") -split ";" | Where-Object { $_ -ne "" })
if ($entries -notcontains $target) {
    [Environment]::SetEnvironmentVariable("Path", (($entries + $target) -join ";"), "User")
    Write-Host "Added $target to your user PATH. Open a new terminal to use 'w'."
} else {
    Write-Host "$target is already on your PATH."
}

Write-Host "Installed: $(& (Join-Path $target 'w.exe') version)"
