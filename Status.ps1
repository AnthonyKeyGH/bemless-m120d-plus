$Spanish = [Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName -eq 'es'
function T($en,$es) { if ($Spanish) { $es } else { $en } }

$log = Join-Path $env:LOCALAPPDATA 'BEMless\BEMless.log'

Write-Host ""
Write-Host "BEMless for ALSEYE M120D Plus" -ForegroundColor Cyan
Write-Host "=============================="
Write-Host ""

Write-Host (T "PROCESS" "PROCESO") -ForegroundColor Yellow
Get-Process BEMless -ErrorAction SilentlyContinue | Select-Object ProcessName, Id, Path | Format-Table -AutoSize

Write-Host (T "SCHEDULED TASK" "TAREA PROGRAMADA") -ForegroundColor Yellow
Get-ScheduledTask -TaskName 'BEMless' -ErrorAction SilentlyContinue | Select-Object TaskName, State | Format-Table -AutoSize

Write-Host "PawnIO" -ForegroundColor Yellow
$paths = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO','HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO')
$found = $false
foreach ($p in $paths) {
    $item = Get-ItemProperty $p -ErrorAction SilentlyContinue
    if ($item) { Write-Host (T "Installed: " "Instalado: ")$item.DisplayVersion; $found = $true; break }
}
if (-not $found) { Write-Host (T "Not detected by uninstall registry." "No detectado en el registro de desinstalación.") }

Write-Host ""
Write-Host (T "LAST LOG LINES" "ÚLTIMAS LÍNEAS DEL LOG") -ForegroundColor Yellow
if (Test-Path $log) { Get-Content $log -Tail 60 } else { Write-Host (T "No log file yet." "Todavía no existe un archivo de log.") }

Write-Host ""
Read-Host (T "Press Enter to close" "Presiona Enter para cerrar")
