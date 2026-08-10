# AUDSPLIT

Per-app audio output routing for Windows. Assign each app its own speaker or headphones — Spotify on Bluetooth, Chrome Meet on wired cans.

Uses the same Windows mechanism as **Settings → System → Sound → Volume mixer** (`IAudioPolicyConfigFactory`).

## Requirements

- Windows 10 1809+ / Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build

## Build & run

```powershell
dotnet build AUDSPLIT.sln -c Release
dotnet run --project src/AUDSPLIT -c Release
```

Or launch:

`src\AUDSPLIT\bin\Release\net8.0-windows\AUDSPLIT.exe`

Smoke-test APIs (no UI):

```powershell
dotnet run --project src/AUDSPLIT -c Release -- --smoke
```

## Usage

1. AUDSPLIT lives in the **system tray**.
2. **Left-click** to open the flyout.
3. Start audio in the apps you want to route, then **Refresh**.
4. Use each app’s dropdown to pick an output (or **System default**).
5. **Reset all** clears every persisted per-app route.

Right-click tray: Open / Refresh / Exit.

## Notes

- Routes are persisted by Windows and survive restarts.
- Some apps only pick up a new device after their stream restarts (pause/play, rejoin Meet).
- Only apps with an active or recent audio session appear.

## License

MIT. AudioPolicyConfig approach adapted from [EarTrumpet](https://github.com/File-New-Project/EarTrumpet) (MIT).
