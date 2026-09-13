# Regenerates COMMANDS.md from the command table in the code.
# A test fails when the file and the code disagree, so run this after adding or changing a command.

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$project = Join-Path $root "src/Wctl/Wctl.csproj"

dotnet build $project -c Release --nologo -v quiet | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

$lines = & dotnet run --project $project -c Release --no-build -- help --markdown
if ($LASTEXITCODE -ne 0) { throw "w help --markdown failed with exit code $LASTEXITCODE" }

$text = ($lines -join "`n") + "`n"
[System.IO.File]::WriteAllText((Join-Path $root "COMMANDS.md"), $text, [System.Text.UTF8Encoding]::new($false))
Write-Host "COMMANDS.md updated"
