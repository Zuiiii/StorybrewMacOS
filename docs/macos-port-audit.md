# macOS port audit

## Pinned baseline

- Storybrew: `204703e6b7b03b0e85f04e79fceea815178a09e4`
- BrewLib: `32bf3f9aac68f00d5ba4d43806d24ec76b3fc4a0`
- Runtime used for the spike: .NET 8.0.10, SDK 8.0.403, `osx-arm64`

## OpenTK 2 decision gate

The isolated `tools/OpenTK2MacSpike` project uses the exact upstream dependency, OpenTK 2.0.0. On an Apple M3 Pro it successfully:

- ran as an arm64 process without Rosetta;
- loaded SDL2 from the arm64 Homebrew prefix through an explicit `DllImportResolver`;
- created a `GameWindow` and OpenGL context;
- rendered and swapped buffers for 342 frames;
- resized to a 1920 x 1080 framebuffer; and
- closed cleanly.

Observed driver strings:

```text
OpenGL vendor: Apple
OpenGL renderer: Apple M3 Pro
OpenGL version: 2.1 Metal - 90.5
GLSL version: 1.20
```

Keyboard, mouse-button, and wheel handlers are instrumented, but require an interactive run to certify. Retina logical-size versus framebuffer-size behavior also needs a dedicated follow-up because OpenTK 2 reports framebuffer dimensions through this SDL backend.

**Decision:** retain OpenTK 2 for the initial port. There is currently no demonstrated blocker justifying an OpenTK 4 migration or public scripting API break.

The NuGet package emits `NU1701` because its assembly is packaged for .NET Framework rather than a modern target framework. It nevertheless loaded and rendered under .NET 8; keep this warning visible until the compatibility surface has broader test coverage.

## Current build blockers

An unmodified `dotnet build storybrew.sln -c Release` fails on macOS with `NETSDK1100` because `editor`, `common`, the generated script project, and BrewLib target `net8.0-windows`; BrewLib also uses the Windows Desktop SDK.

Windows-specific usage is concentrated in seven source files:

- editor dialogs, startup/crash handling, updater, builder, and `IWin32Window` plumbing;
- BrewLib clipboard and textbox `TextDataFormat` usage.

The source audit found 35 C# files referencing `System.Drawing`. The scripting boundary is not merely incidental: `StoryboardObjectGenerator.GetProjectBitmap` and `GetMapsetBitmap` publicly return `Bitmap`, and font effects expose `Bitmap`, `Graphics`, and `Font`. These signatures must remain source-compatible while the implementation moves to a supported raster backend.

Other native/platform blockers found:

- `brewlib/Util/Native.cs` imports `msvcrt.dll` and `user32.dll`;
- `editor/Util/Native.cs` imports `kernel32.dll`;
- BrewLib packages only Windows `bass.dll` and `bass_fx.dll`;
- `System.Management` is referenced by the BrewLib project and must be proven necessary or removed;
- the generated `scripts.csproj` targets Windows and has a direct `System.Drawing.Common.dll` reference.

## Required implementation order

1. Split shared `net8.0` targets from conditional Windows implementations while preserving an upstream-compatible Windows build.
2. Introduce narrow platform services for dialogs, clipboard, launching, paths, and window/monitor operations.
3. Replace `msvcrt` memory calls with runtime intrinsics or portable unsafe code and isolate Win32 calls.
4. Implement the measured bitmap compatibility surface over SkiaSharp without changing public script signatures.
5. Package and validate arm64 BASS/BASS_FX libraries with explicit diagnostics.
6. Port generated script compilation and use RID-separated disposable caches.
7. Validate a real bidirectional Windows/macOS project round trip before claiming compatibility.

No renderer, storyboard format, batching system, or public OpenTK type should be migrated during these steps.
