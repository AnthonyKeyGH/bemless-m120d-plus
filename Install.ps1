$ErrorActionPreference = 'Stop'

$Spanish = [Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName -eq 'es'
function T([string]$English, [string]$SpanishText) { if ($Spanish) { return $SpanishText }; return $English }

Write-Host ""
Write-Host "BEMless for ALSEYE M120D Plus" -ForegroundColor Cyan
Write-Host "=============================="
Write-Host ""
Write-Host (T "A lightweight, headless Windows replacement for ALSEYE BEM Gen I." "Un reemplazo ligero y sin interfaz para ALSEYE BEM Gen I en Windows.") -ForegroundColor Gray
Write-Host (T "No UI. No unnecessary crap. Just CPU temperature sent straight to the display." "Sin interfaz. Sin cosas innecesarias. Solo la temperatura del CPU directo a la pantalla.") -ForegroundColor Gray
Write-Host ""

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $env:TEMP 'BEMless-publish'
$sdkDir = Join-Path $env:LOCALAPPDATA 'BEMless-dotnet-sdk'
$elevatedScript = Join-Path $projectDir 'Install-Elevated.ps1'

function Find-DotNet {
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    $local = Join-Path $sdkDir 'dotnet.exe'
    if (Test-Path $local) { return $local }
    return $null
}

$dotnet = Find-DotNet

if (-not $dotnet) {
    Write-Host (T "A .NET 8 SDK was not found. Downloading Microsoft's installer..." "No se encontró .NET 8 SDK. Descargando el instalador de Microsoft...") -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $sdkDir | Out-Null
    $dotnetInstaller = Join-Path $env:TEMP 'dotnet-install.ps1'
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile $dotnetInstaller
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $dotnetInstaller -Channel 8.0 -InstallDir $sdkDir -NoPath
    if ($LASTEXITCODE -ne 0) { throw (T 'The .NET 8 SDK could not be installed.' 'No se pudo instalar .NET 8 SDK.') }
    $dotnet = Join-Path $sdkDir 'dotnet.exe'
}

Write-Host (T "Building BEMless..." "Compilando BEMless...") -ForegroundColor Cyan
Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

& $dotnet restore (Join-Path $projectDir 'BEMless.csproj')
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

& $dotnet publish (Join-Path $projectDir 'BEMless.csproj') -c Release -r win-x64 --self-contained true --no-restore -o $publishDir
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

$targetUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$args = @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"' + $elevatedScript + '"'),'-PublishDir',('"' + $publishDir + '"'),'-TargetUser',('"' + $targetUser + '"')) -join ' '

Write-Host ""
Write-Host (T "Build complete. Windows will ask for administrator permission once." "Compilación terminada. Windows pedirá permisos de administrador una sola vez.") -ForegroundColor Yellow

$p = Start-Process powershell.exe -Verb RunAs -ArgumentList $args -Wait -PassThru
if ($p.ExitCode -ne 0) { throw (T "Installation failed with exit code $($p.ExitCode)." "La instalación falló con el código $($p.ExitCode).") }

Write-Host ""
Write-Host (T "Done. Restart Windows if PawnIO was installed for the first time." "Listo. Reinicia Windows si PawnIO se instaló por primera vez.") -ForegroundColor Green
Pause
