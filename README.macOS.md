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
