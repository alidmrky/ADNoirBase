$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
function New-Secret {
    $bytes = New-Object byte[] 48
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return [Convert]::ToBase64String($bytes)
}
$envPath = Join-Path $root '.env'
if (!(Test-Path $envPath)) {
    $password = New-Secret
    $content = [IO.File]::ReadAllText((Join-Path $root '.env.example'))
    $content = $content.Replace('POSTGRES_PASSWORD=', "POSTGRES_PASSWORD=$password")
    $content = $content.Replace('Password=', "Password=$password")
    $content = $content.Replace('Jwt__SecretKey=', ('Jwt__SecretKey=' + (New-Secret)))
    $content = $content.Replace('ADMIN_PASSWORD=', ('ADMIN_PASSWORD=' + (New-Secret)))
    $content = $content.Replace('Notification__UnsubscribeSecret=', ('Notification__UnsubscribeSecret=' + (New-Secret)))
    [IO.File]::WriteAllText($envPath, $content)
}
$uiEnv = Join-Path $root 'ui/.env.local'
if (!(Test-Path $uiEnv)) {
    $content = [IO.File]::ReadAllText((Join-Path $root 'ui/.env.local.example'))
    [IO.File]::WriteAllText($uiEnv, $content.Replace('AUTH_SECRET=', ('AUTH_SECRET=' + (New-Secret))))
}
Write-Host 'Yerel ortam dosyalari hazir. Mevcut dosyalar korunmustur; gizli degerler .env ve ui/.env.local icindedir.'
