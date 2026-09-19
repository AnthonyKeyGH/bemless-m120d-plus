# Security

BEMless is an unsigned community utility for **Windows** and the tested **ALSEYE M120D Plus**.

It reads CPU sensor data through LibreHardwareMonitor/PawnIO, writes a small output report to the M120D Plus vendor-defined USB HID device, and runs through an elevated Windows Scheduled Task.

## Recommended installation
Use the repository source and run `Install.bat`.

## Device safety
BEMless does not flash firmware. It only sends the temperature report confirmed on the tested M120D Plus.

Do not assume unrelated hardware using the same VID/PID is compatible.
