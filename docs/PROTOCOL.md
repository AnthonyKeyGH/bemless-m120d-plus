# BEMless — ALSEYE M120D Plus HID protocol notes

## Scope
Tested on **ALSEYE M120D Plus** on Windows using the BEM Gen I protocol.

```text
VID: 0x5131
PID: 0x2007
```

Do not assume the M120D (non-Plus), other BEM Gen I products, BEM Gen II, BEM Gen III, or unrelated hardware using the same VID/PID uses this protocol.

## Temperature report

| Offset | Value | Meaning |
| --- | --- | --- |
| 0 | `0x00` | HID Report ID |
| 1 | `0x40` | Command |
| 2 | `0x00`–`0x63` | Displayed temperature in Celsius |
| 3–64 | `0x00` in BEMless | Not used by the temperature-only implementation |

Example for decimal 47 (`0x2F`):
```text
00 40 2F 00 00 00 ...
```

A manual test writing decimal `55` to byte 2 caused the physical M120D Plus display to show `55`.

BEMless refreshes the report about once per second.
