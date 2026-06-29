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

## Save file location (same as the game)

Unity `Application.persistentDataPath` on Windows:

`%LOCALAPPDATA%/../LocalLow/Midnight Line Studio/Midnight Metro/`

- Primary slot: `midnight_metro_save.json`
- Autosaves: `Saves/<CityName>/autosave_*.json`

## Editing game saves

The editor loads the **Midnight Metro game schema** (`MetroSaveFile` — synced from the game repo).

Editable areas:

- Session (day, city name, treasury, seeds, toggles)
- Citizens (residents + deceased archive)
- Grid cells (type, zone, population, wealth, lots)
- Raw JSON (advanced)

On save, edits are merged back into the original JSON so **unknown/new fields** from newer game versions are preserved until you run schema sync.

### Keep schemas in sync

When the game changes save format, run from repo root:

```powershell
./scripts/sync-save-schema.ps1
```

This updates:

- `Models/MetroSaveModels.cs` from `Assets/MidnightMetro/Scripts/Simulation/Saves/SaveModels.cs`
- `Models/MetroSaveSchema.cs` `CurrentSaveVersion` from `GameSession.CaptureSave` (`file.version = N`)

If a save reports a version newer than `MetroSaveSchema.CurrentSaveVersion`, sync the schema before editing.

## CLI tools

Verify (structural checks + merge round-trip for game saves):

```bash
dotnet run -c Release -- --verify "C:\path\to\save.json"
```

Reset metro network only (surgical patch — does not touch residents):

```bash
dotnet run -c Release -- --reset-metro "C:\path\to\save.json"
```

## Legacy prototype saves

Older `CitySimSaveFile` / `playground-1` saves remain fully editable with the legacy UI (budget, gangs, agents, etc.).

## Citizen name resolution

Configure `citizen_names.json` in `%AppData%\MidnightMetroEditor\settings.json` or rely on auto-detection from sibling Unity projects.

## Safety

Always back up saves before editing. The editor writes a `.bak` copy beside the file on save.

## License

Apache License 2.0 — see [LICENSE](LICENSE).
