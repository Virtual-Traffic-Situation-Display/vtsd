# vTFMS — Virtual Traffic Flow Management System

A desktop radar-style traffic situation display for [VATSIM](https://vatsim.net), built with .NET 10 and Avalonia UI.

## Features

- Live VATSIM pilot tracking with configurable flight filters
- US state, country, and TRACON boundary overlays
- Airport, VOR, NDB, and waypoint map items
- NOAA MRMS radar weather overlay
- Customizable display colors and fonts
- Saveable/loadable filter profiles

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Linux (Arch / CachyOS)

```bash
sudo pacman -S dotnet-sdk
```

> **Note:** The UI uses Courier New for data blocks and labels. Install `ttf-corefonts` from the AUR for correct rendering, or the system will substitute a fallback monospace font.

### Windows

Install the .NET 10 SDK from the link above.

## Build & Run

```bash
dotnet build
dotnet run --project vTFMS
```

## Keyboard Shortcuts (Radar Display)

| Key | Action |
|-----|--------|
| M   | Center map on mouse cursor |
| Z   | Zoom in |
| U   | Zoom out |

## Architecture

- **Framework:** Avalonia UI 11.3 (cross-platform)
- **Pattern:** MVVM with CommunityToolkit.Mvvm
- **Data:** FAA aviation data (CSV), Natural Earth boundaries (GeoJSON), VATSIM JSON API
- **Weather:** NOAA WMS radar imagery

## License

This project is licensed under [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/). You are free to fork and modify, but you must credit the original authors, keep derivatives under the same license, and may not use it commercially.
