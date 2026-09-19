$Spanish = [Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName -eq 'es'
function T($en,$es) { if ($Spanish) { $es } else { $en } }

Write-Host ""
Write-Host (T "BEMless will be removed. PawnIO will be left installed because other monitoring apps may use it." "Se eliminará BEMless. PawnIO se conservará porque otras aplicaciones de monitoreo pueden utilizarlo.") -ForegroundColor Yellow

$command = @"
Unregister-ScheduledTask -TaskName 'BEMless' -Confirm:`$false -ErrorAction SilentlyContinue
Get-Process BEMless -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Remove-Item '`$env:ProgramFiles\BEMless' -Recurse -Force -ErrorAction SilentlyContinue
"@

$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))
Start-Process powershell.exe -Verb RunAs -ArgumentList "-NoProfile -EncodedCommand $encoded" -Wait

Write-Host (T "BEMless was removed." "BEMless fue eliminado.") -ForegroundColor Green
Write-Host (T "The display keeps the last value it received." "La pantalla conserva el último valor que recibió.")
Read-Host (T "Press Enter to close" "Presiona Enter para cerrar")
