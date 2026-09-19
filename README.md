# BEMless for ALSEYE M120D Plus

[Español](README.es.md)

A lightweight, headless replacement for **ALSEYE BEM Gen I on the ALSEYE M120D Plus for Windows**.

**No UI. No unnecessary crap. Just CPU temperature sent straight to the display.**

> Unofficial community project. Not affiliated with or endorsed by ALSEYE.

## Platform

- Windows 10 x64
- Windows 11 x64

This project is **Windows-only**. It uses native Windows HID and SetupAPI APIs and runs through a Windows Scheduled Task.

## Confirmed compatibility

Confirmed on the hardware used during development:

- **ALSEYE M120D Plus**
- ALSEYE **BEM Gen I** protocol
- USB HID `VID 0x5131 / PID 0x2007`
- Vendor-defined HID interface
- Tested with an AMD Ryzen 5 7600X on Windows 11 x64

Not currently confirmed:

- ALSEYE M120D (non-Plus)
- Other BEM Gen I coolers
- BEM Gen II
- BEM Gen III
- Unrelated devices that happen to reuse `5131:2007`

If another model is eventually verified, it can be added to the compatibility list without changing the M120D Plus baseline.

## Why BEMless?

For the M120D Plus, the goal is simple: put the CPU temperature on the cooler display without keeping the full BEM user interface running.

```text
CPU temperature
      ↓
   BEMless
      ↓
    USB HID
      ↓
M120D Plus display
```

## Features

- Headless: no normal UI.
- Starts automatically with Windows.
- No UAC prompt on every login.
- Windows UI language detection:
  - Spanish Windows → Spanish installer, utilities, and logs.
  - Any other language → English.
- Installs PawnIO automatically through WinGet when needed.
- Reads CPU temperature using LibreHardwareMonitor.
- Prioritizes `Core (Tctl/Tdie)` on AMD Ryzen.
- Updates the display about once per second.
- Never sends `0 °C` when the sensor is unavailable.
- Does not require ALSEYE BEM to remain installed.
- Celsius only, matching the tested two-digit M120D Plus display.

## Installation

1. Download or clone this repository.
2. Run `Install.bat`.
3. Approve the single administrator/UAC prompt.
4. Restart Windows if PawnIO was installed for the first time.

The installer will:

1. obtain a local .NET 8 SDK if needed;
2. compile BEMless from source;
3. install PawnIO through WinGet if missing;
4. install BEMless under:

```text
C:\Program Files\BEMless
```

5. create an elevated Scheduled Task named:

```text
BEMless
```

The Scheduled Task allows hardware sensor access without showing a UAC prompt on every Windows login.

## Windows language detection

BEMless uses the Windows UI culture automatically.

```text
es-MX → Spanish
es-ES → Spanish
en-US → English
de-DE → English
```

There is no language setting to manage.

## Reverse-engineered HID protocol

For the tested ALSEYE M120D Plus:

```text
VID = 0x5131
PID = 0x2007
Report length = 65 bytes

Byte 0 = 0x00
Byte 1 = 0x40
Byte 2 = temperature in °C
```

Example for `55 °C`:

```text
00 40 37 00 00 00 ...
```

A physical test sending decimal `55` to byte 2 caused the M120D Plus display to show `55`.

See [docs/PROTOCOL.md](docs/PROTOCOL.md).

## Utilities

### `Status.bat`

Shows whether BEMless is running, the Scheduled Task state, PawnIO status, and recent log entries.

### `Restore-24.bat`

Sends `24` to the display one time.

### `Uninstall.bat`

Removes BEMless and its Scheduled Task.

PawnIO is intentionally left installed because other hardware-monitoring software may use it.

## Log

```text
%LOCALAPPDATA%\BEMless\BEMless.log
```

The log automatically follows the Windows UI language.

## Build manually

Requirements:

- Windows 10/11 x64
- .NET 8 SDK
- PawnIO for reliable low-level CPU sensor access

```powershell
dotnet restore BEMless.csproj
dotnet publish BEMless.csproj -c Release -r win-x64 --self-contained true
```

## Security

The repository contains source code and scripts. The normal source installation builds BEMless locally.

The application writes only the reverse-engineered temperature HID report; it does not flash display firmware.

See [SECURITY.md](SECURITY.md).

## License

BEMless original code is released under the MIT License.

Third-party dependencies keep their own licenses. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
