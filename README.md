# vTFMS — Virtual Traffic Flow Management System

A desktop radar-style traffic situation display for [VATSIM](https://vatsim.net), built with .NET 10 and Avalonia UI. Designed to emulate the look and feel of FAA TFMS/TSD workstations for virtual ATC and flight tracking.

![Release](https://img.shields.io/github/v/release/Virtual-Traffic-Situation-Display/vtsd?include_prereleases)
![License: CC BY-NC-SA 4.0](https://img.shields.io/badge/license-CC%20BY--NC--SA%204.0-blue)

## Features

- **Live VATSIM tracking** — real-time pilot positions with configurable flight filters, altitude filtering, route drawing, and a live flight count display
- **Map overlays** — US state boundaries, country boundaries, TRACON boundaries, ARTCC sector boundaries, and airways, all toggled independently
- **Aviation data** — airports, VORs, NDBs, and waypoints from FAA source data
- **Weather radar** — NOAA MRMS radar imagery overlay
- **NAS Monitor** — sector-based traffic count monitor with configurable ARTCC thresholds and TRACON monitoring
- **Range rings** — centered on any identifier with configurable interval and distance
- **Display customization** — fully configurable colors, fonts, and map appearance via the Adapt panel
- **Filter profiles** — save and load flight filters along with map position and active overlays

## Downloads

Pre-built binaries for Windows, Linux, and macOS are available on the [Releases](https://github.com/Virtual-Traffic-Situation-Display/vtsd/releases) page.

| Platform | Artifact |
|----------|----------|
| Windows (x64) | `vTFMS-win-x64.zip` |
| Linux (x64) | `vTFMS-linux-x64.zip` |
| macOS (Intel) | `vTFMS-osx-x64.zip` |
| macOS (Apple Silicon) | `vTFMS-osx-arm64.zip` |

## Building from Source

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

#### Linux (Arch / CachyOS)

```bash
sudo pacman -S dotnet-sdk
```

> **Note:** The UI uses Courier New for data blocks and labels. Install `ttf-corefonts` from the AUR for correct rendering, or the system will substitute a fallback monospace font.

#### Windows / macOS

Install the .NET 10 SDK from the link above.

### Build & Run

```bash
dotnet build
dotnet run --project vTFMS
```

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| M | Center map on mouse cursor |
| Z | Zoom in |
| U | Zoom out |

## Menu Reference

| Menu | Items |
|------|-------|
| **Display** | Adapt (colors/fonts), Profiles (save/load — coming soon), Filters (save/load) |
| **Maps** | Move/Zoom, Show Map Item, Range Rings, Overlays |
| **Flights** | Enable VATSIM Data, Display All Aircraft, Altitude Filter, Select Flights, Flight Count |
| **Alerts** | NAS Monitor |
| **Weather** | Select Weather |
| **FEA/FCA** | *(coming soon)* |
| **Help** | About |

## Architecture

- **Framework:** Avalonia UI 11.3 (cross-platform desktop)
- **Pattern:** MVVM with CommunityToolkit.Mvvm
- **Data:** FAA aviation data (CSV), Natural Earth boundaries (GeoJSON), VATSIM JSON API
- **Weather:** NOAA WMS radar imagery
- **CI/CD:** GitHub Actions — automatic build on push/PR, release workflow on version tags

## Community

- [Discord](https://discord.gg/a3Br8KqcJ4)
- [Bug Reports](https://github.com/Virtual-Traffic-Situation-Display/vtsd/issues/new?template=bug_report.yml)
- [Feature Requests](https://github.com/Virtual-Traffic-Situation-Display/vtsd/issues/new?template=feature_request.yml)

## License

This project is licensed under [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/). You are free to fork and modify, but you must credit the original authors, keep derivatives under the same license, and may not use it commercially.
