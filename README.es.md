# BEMless para ALSEYE M120D Plus

[English](README.md)

Un reemplazo ligero y sin interfaz para **ALSEYE BEM Gen I en el ALSEYE M120D Plus para Windows**.

**Sin interfaz. Sin cosas innecesarias. Solo la temperatura del CPU directo a la pantalla.**

> Proyecto comunitario no oficial. No está afiliado ni respaldado por ALSEYE.

## Plataforma
- Windows 10 x64
- Windows 11 x64

Este proyecto es **exclusivamente para Windows**.

## Compatibilidad confirmada
- **ALSEYE M120D Plus**
- Protocolo ALSEYE **BEM Gen I**
- USB HID `VID 0x5131 / PID 0x2007`
- Interfaz HID definida por el fabricante
- Probado con un AMD Ryzen 5 7600X en Windows 11 x64

No confirmado actualmente:
- ALSEYE M120D sin Plus
- Otros coolers BEM Gen I
- BEM Gen II
- BEM Gen III

## Características
- Sin interfaz normal.
- Inicia automáticamente con Windows.
- No muestra UAC en cada inicio.
- Español automático en Windows `es-*`; inglés en otros idiomas.
- Instala PawnIO mediante WinGet si hace falta.
- Lee la temperatura mediante LibreHardwareMonitor.
- Nunca envía `0 °C` si el sensor no entrega una lectura válida.
- No requiere mantener instalado ALSEYE BEM.
- Solo Celsius.

## Instalación
1. Descarga o clona el repositorio.
2. Ejecuta `Install.bat`.
3. Acepta el único aviso UAC.
4. Reinicia Windows si PawnIO se instaló por primera vez.

BEMless se instala en:
```text
C:\Program Files\BEMless
```

## Protocolo HID
Para el ALSEYE M120D Plus probado:
```text
VID = 0x5131
PID = 0x2007
Longitud = 65 bytes
Byte 0 = 0x00
Byte 1 = 0x40
Byte 2 = temperatura en °C
```

Ejemplo para `55 °C`:
```text
00 40 37 00 00 00 ...
```

Consulta [docs/PROTOCOL.md](docs/PROTOCOL.md).

## Utilidades
- `Status.bat`: proceso, tarea, PawnIO y log.
- `Restore-24.bat`: envía 24 una vez.
- `Uninstall.bat`: elimina BEMless y la tarea.

## Log
```text
%LOCALAPPDATA%\BEMless\BEMless.log
```

## Licencia
Código propio bajo MIT. Dependencias de terceros conservan sus licencias. Consulta [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
