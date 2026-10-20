# Storybrew for macOS (Apple Silicon)

## Build

Install .NET 8 and SDL2, then run:

```bash
brew install sdl2
./scripts/build-macos-app.sh
```

The native application is created at `artifacts/Storybrew.app`. It is framework-dependent and uses the regular .NET 8 arm64 JIT; it is neither Native AOT nor single-file.

## Run

```bash
open artifacts/Storybrew.app
```

The build downloads the official macOS BASS and BASS_FX libraries, verifies their checksums and arm64 slices, and bundles them with an arm64 SDL2 library. No Wine, Mono, XQuartz, Rosetta, or Windows runtime is used.

## Working with osu!lazer

In lazer, export the map using **For compatibility (.osz)**. When creating a
Storybrew project, select that `.osz` as the mapset. In an existing project,
right-click the mapset folder button and select the `.osz`.

Storybrew extracts a permanent working copy into the project's `mapsets` folder
and saves that location. The original export can then be moved or deleted.
Importing again creates a separate copy and keeps earlier storyboard edits.
Selecting a `.osu` continues to use its existing folder directly.

Preview changes in Storybrew. To view them in lazer, package the edited mapset
as an `.osz` and import it back into lazer; its internal hashed storage is not
edited or synchronized by Storybrew.

## Script graphics on macOS

Font textures use Skia on macOS, including outline, shadow, glow, background,
and gradient effects. The public font configuration stays the same.
For image dimensions, use `GetMapsetBitmapInfo(path).Width` and `.Height`;
`GetMapsetBitmap` returns a Windows-only `System.Drawing.Bitmap`.

Skia output is not guaranteed to match Windows GDI rasterization exactly.
Custom font effects implemented against `System.Drawing` remain Windows-only.
See [renderer comparison results and limitations](docs/renderer-comparison.md).
