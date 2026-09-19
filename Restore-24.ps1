$Spanish = [Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName -eq 'es'
function T($en,$es) { if ($Spanish) { $es } else { $en } }

$exe = Join-Path $env:ProgramFiles 'BEMless\BEMless.exe'
if (-not (Test-Path $exe)) {
    Write-Host (T "BEMless is not installed." "BEMless no está instalado.") -ForegroundColor Red
    Read-Host (T "Press Enter to close" "Presiona Enter para cerrar")
    exit 1
}

Write-Host (T "Sending 24 to the ALSEYE display one time..." "Enviando 24 a la pantalla ALSEYE una sola vez...")
Start-Process $exe -ArgumentList '--set 24' -Verb RunAs -Wait
Write-Host (T "Done." "Listo.") -ForegroundColor Green
Read-Host (T "Press Enter to close" "Presiona Enter para cerrar")
