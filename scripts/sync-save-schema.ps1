# Sync Midnight Metro game save schema + version into the editor.
# Run from repo root after SaveModels.cs or CaptureSave version changes in the game.

param(
    [string]$GameRepo = (Join-Path $PSScriptRoot "..\..\Midnight Metro\Midnight Metro"),
    [string]$SourceModels = "Assets\MidnightMetro\Scripts\Simulation\Saves\SaveModels.cs",
    [string]$SourceSession = "Assets\MidnightMetro\Scripts\GameSession.cs",
    [string]$TargetModels = "src\MidnightMetroEditor\Models\MetroSaveModels.cs",
    [string]$TargetSchema = "src\MidnightMetroEditor\Models\MetroSaveSchema.cs"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent

$srcModels = Join-Path $GameRepo $SourceModels
$srcSession = Join-Path $GameRepo $SourceSession
$dstModels = Join-Path $repoRoot $TargetModels
$dstSchema = Join-Path $repoRoot $TargetSchema

if (-not (Test-Path $srcModels)) { Write-Error "Game SaveModels not found: $srcModels" }
if (-not (Test-Path $srcSession)) { Write-Error "Game GameSession not found: $srcSession" }

$content = Get-Content -Raw $srcModels
$content = $content -replace '(?m)^using MidnightMetro\.Simulation\.World;\r?\n', ''
$content = $content -replace '(?m)^using System;\r?\n', ''
$content = $content -replace 'namespace MidnightMetro\.Simulation\.Saves', "// AUTO-SYNCED from Midnight Metro SaveModels.cs`r`nnamespace MidnightMetroEditor.Models"
$content = $content -replace '(?m)^\s*\[Serializable\]\r?\n', ''
$content = $content -replace 'CityExpansion\.StartingTreasury', 'MetroSaveSession.DefaultStartingTreasury'
$content = $content -replace 'public class MetroSaveSession\r?\n\s*\{', "public class MetroSaveSession`r`n{`r`n    public const int DefaultStartingTreasury = 500_000;"
$content = $content -replace 'public int\[\] ', 'public int[]? '
$content = $content -replace 'public float\[\] ', 'public float[]? '
$content = $content -replace 'public string\[\] ', 'public string[]? '
$content = $content -replace 'public MetroSaveNewsArticleRow\[\] ', 'public MetroSaveNewsArticleRow[]? '
$content = $content -replace 'public MetroSaveMetroLineRow\[\] ', 'public MetroSaveMetroLineRow[]? '
$content = $content -replace 'public MetroSaveMetroLineLinkRow\[\] ', 'public MetroSaveMetroLineLinkRow[]? '
$content = $content -replace 'public MetroSaveMetroStationRow\[\] ', 'public MetroSaveMetroStationRow[]? '
$content = $content -replace 'public MetroSaveSubwayBuildRow\[\] ', 'public MetroSaveSubwayBuildRow[]? '
$content = $content -replace 'public MetroSaveMetroLineRidershipRow\[\] ', 'public MetroSaveMetroLineRidershipRow[]? '

New-Item -ItemType Directory -Force -Path (Split-Path $dstModels) | Out-Null
Set-Content -Path $dstModels -Value $content -NoNewline

$sessionText = Get-Content -Raw $srcSession
if ($sessionText -match 'file\.version\s*=\s*(\d+)') {
    $version = $Matches[1]
    $schema = @(
        'namespace MidnightMetroEditor.Models;',
        '',
        '/// Save format version constants - synced from GameSession.CaptureSave.',
        'public static class MetroSaveSchema',
        '{',
        "    public const int CurrentSaveVersion = $version;",
        '    public const int MinimumSupportedVersion = 3;',
        '}'
    ) -join "`r`n"
    Set-Content -Path $dstSchema -Value $schema -NoNewline
    Write-Host "Synced save version -> v$version"
} else {
    Write-Warning 'Could not find file.version in GameSession.cs; MetroSaveSchema.cs not updated.'
}

Write-Host "Synced save models -> $dstModels"
