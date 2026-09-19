param(
    [Parameter(Mandatory=$true)][string]$PublishDir,
    [Parameter(Mandatory=$true)][string]$TargetUser
)

$ErrorActionPreference = 'Stop'
$Spanish = [Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName -eq 'es'
function T([string]$English, [string]$SpanishText) { if ($Spanish) { return $SpanishText }; return $English }

$InstallDirectory = Join-Path $env:ProgramFiles 'BEMless'
$Executable = Join-Path $InstallDirectory 'BEMless.exe'
$TaskName = 'BEMless'

function Test-PawnIoInstalled {
    $paths = @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO'
    )
    foreach ($path in $paths) { if (Test-Path $path) { return $true } }
    $service = Get-Service -Name PawnIO -ErrorAction SilentlyContinue
    return $null -ne $service
}

if (-not (Test-PawnIoInstalled)) {
    Write-Host (T "PawnIO is required for reliable CPU sensor access." "PawnIO es necesario para acceder de forma confiable a los sensores del CPU.") -ForegroundColor Yellow
    $winget = Get-Command winget.exe -ErrorAction SilentlyContinue
    if (-not $winget) { throw (T "WinGet was not found. Install App Installer from Microsoft and run Install.bat again." "No se encontró WinGet. Instala App Installer de Microsoft y vuelve a ejecutar Install.bat.") }
    & $winget.Source install --id namazso.PawnIO --exact --source winget --silent --accept-package-agreements --accept-source-agreements --disable-interactivity
    if ($LASTEXITCODE -ne 0) { throw (T "PawnIO installation through WinGet failed." "Falló la instalación de PawnIO mediante WinGet.") }
    Start-Sleep -Seconds 2
}

Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue
Get-Process BEMless -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 700

New-Item -ItemType Directory -Force -Path $InstallDirectory | Out-Null
Get-ChildItem $InstallDirectory -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $PublishDir '*') $InstallDirectory -Recurse -Force

if (-not (Test-Path $Executable)) { throw (T 'BEMless.exe was not copied to Program Files.' 'BEMless.exe no se copió a Program Files.') }

$action = New-ScheduledTaskAction -Execute $Executable
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $TargetUser
$principal = New-ScheduledTaskPrincipal -UserId $TargetUser -LogonType Interactive -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -MultipleInstances IgnoreNew -ExecutionTimeLimit ([TimeSpan]::Zero)

Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings | Out-Null
Start-ScheduledTask -TaskName $TaskName

Write-Host ""
Write-Host (T "BEMless installed." "BEMless instalado.") -ForegroundColor Green
Write-Host $Executable
exit 0
