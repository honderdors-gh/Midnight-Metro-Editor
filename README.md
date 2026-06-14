# Midnight Metro Save Editor

Windows desktop tool for inspecting and editing **Midnight Metro** save files (gzip-compressed JSON).

By [Midnight Line Studio](https://github.com/honderdors-gh). Game repo: [Midnight-Metro](https://github.com/honderdors-gh/Midnight-Metro).

## Requirements

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or newer
- Windows (WinForms)

## Build

```bash
cd src/MidnightMetroEditor
dotnet build -c Release
```

Run:

```bash
dotnet run -c Release
# or
./bin/Release/net6.0-windows/MidnightMetroEditor.exe
```

## CLI verify mode

Round-trip a save without opening the UI:

```bash
dotnet run -c Release -- --verify "C:\path\to\save.json"
```

## Citizen name resolution

The editor resolves roster IDs to readable names when it can find `citizen_names.json` from the game project. Configure a custom path in the app settings (stored under `%AppData%\MidnightMetroEditor\settings.json`) if auto-detection fails.

## Safety

Always back up saves before editing. The editor writes a `.bak` copy beside the file on save when possible.

## License

Apache License 2.0 — see [LICENSE](LICENSE).
