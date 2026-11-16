<h1><img src="icons/ArchitecturalAtelierTools.svg" height="40" align="top"> ArchitecturalAtelierTools</h1>

Rhino 8 tools for architectural drawings.

| Icon | Command | What it does |
|------|---------|--------------|
| <img src="icons/SoftInsulation.svg" width="32"> | `SoftInsulation` | Draws the soft insulation symbol along a curve. |
| <img src="icons/AATDim.svg" width="32"> | `AATDim` | Multi-row dimension chains: `80` below 1 m, `1.20` from 1 m. |
| <img src="icons/AATPreferences.svg" width="32"> | `AATPreferences` | Sets the layer each tool draws on. |

## Install

Download the `.yak` from [Releases](../../releases) and drag it into Rhino 8, or install `ArchitecturalAtelierTools` via `_PackageManager`. Restart Rhino.

---

## SoftInsulation

![Loop construction](docs/principle-loops.svg)

1. Run `SoftInsulation`.
2. Pick a curve, or press **Enter** to draw a polyline.
3. Type the band width and press **Enter**.

Options: `Spacing` (0 = auto), `Corner` (`Miter`, `ButtIncoming`, `ButtOutgoing`), `CrossingLoops` (`Trim`, `Remove`), `Rhythm` (`Continuous`, `Restart`).

![Corner modes](docs/principle-corners.svg)

---

## AATDim

![AATDim chain and its buttons](docs/aatdim-chain.svg)

1. Run `AATDim`, set `Scale` (`1_100`, `1_50`, `1_25`) and `Rows`.
2. Pick the points, press **Enter**.
3. Click where the first dimension line goes.

Every row gets all points; the last row is the overall dimension.

**Buttons on a selected chain**

- **`<>`** move a measure point (in every row)
- **`+`** (green) at both ends of a row – extend the row with more points, **Enter** when done
- **`x`** remove a point from a row
- **`-`** delete a row
- **`+`** add a row
- **`+` / `T`** (orange) add or edit a text below a dimension, e.g. `RPH 2.10`
- **`...`** chain menu: row spacing and texts below

**Styles**: `AAT_Bemassung_1-100`, `AAT_Bemassung_1-50`, `AAT_Bemassung_1-25` – Arial, ticks, 2 mm text on paper. The chosen one becomes current, so `_Dim` uses it too.

Dimensions are normal Rhino dimensions, grouped per chain. Moving a point or a dimension line updates the whole chain. To keep this active from the start, add `AATDim _Update` to *Options → General → Run these commands every time Rhino starts*.

---

## Settings

Run `AATPreferences` to set the layer per tool. Further defaults can be set in `SoftInsulation.json` / `AATDim.json` in `%APPDATA%\ArchitecturalAtelierTools\`.

## Development

```
commands/   one .cs file per command
icons/      plugin and command icons
docs/       drawings for this README
build.ps1   builds the .yak package
```

Release (Rhino must be running):

```powershell
.\build.ps1 -Version 1.1.0 -Feed "\\SERVER\share\RhinoPackages"
```

Open for forking – contributions welcome.
