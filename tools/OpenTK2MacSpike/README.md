# OpenTK 2 macOS arm64 compatibility spike

This executable isolates the exact `OpenTK` 2.0.0 package used by Storybrew. It verifies native arm64 execution, creates a `GameWindow` and OpenGL context, draws a triangle, swaps buffers, and logs keyboard, mouse, wheel, resize, and clean-close events.

```bash
dotnet run --project tools/OpenTK2MacSpike/OpenTK2MacSpike.csproj -c Release
```

Press Escape to close. SDL2 must be an arm64 dylib in the output directory or a standard Homebrew prefix.

For unattended context/render/clean-close validation:

```bash
STORYBREW_SPIKE_AUTO_CLOSE_SECONDS=3 dotnet run --project tools/OpenTK2MacSpike/OpenTK2MacSpike.csproj -c Release
```
