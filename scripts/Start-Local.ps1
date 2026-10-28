param([ValidateSet('Api', 'Ui', 'Migrate')][string]$Target = 'Api')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (!(Test-Path (Join-Path $root '.env'))) { throw 'Once scripts/Initialize-Local.ps1 calistirin.' }
Get-Content (Join-Path $root '.env') | ForEach-Object {
    if ($_ -match '^\s*([^#\s][^=]*)=(.*)$') {
        [Environment]::SetEnvironmentVariable($Matches[1].Trim(), $Matches[2].Trim(), 'Process')
    }
}
Push-Location $root
try {
    switch ($Target) {
        'Api' { dotnet run --project src/ADNoirBase.API --launch-profile http }
        'Migrate' { dotnet run --project src/Tools/ADNoirBase.DbMigrator }
        'Ui' { Set-Location ui; npm.cmd run dev -- --port 3000 }
    }
    if ($LASTEXITCODE -ne 0) { throw "Komut basarisiz: $LASTEXITCODE" }
} finally { Pop-Location }
