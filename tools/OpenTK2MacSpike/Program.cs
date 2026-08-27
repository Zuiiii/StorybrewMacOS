using System.Reflection;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Diagnostics;

if (!OperatingSystem.IsMacOS())
    throw new PlatformNotSupportedException("This compatibility spike is specifically for macOS.");

NativeLibrary.SetDllImportResolver(typeof(GameWindow).Assembly, ResolveOpenTkNativeLibrary);

Console.WriteLine($"OS architecture: {RuntimeInformation.OSArchitecture}");
Console.WriteLine($"Process architecture: {RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"Runtime identifier: {RuntimeInformation.RuntimeIdentifier}");
if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
    throw new PlatformNotSupportedException("The spike must run as a native arm64 process.");

using var window = new GameWindow(960, 540, GraphicsMode.Default, "OpenTK 2 macOS arm64 spike");
var inputSeen = false;
var renderedFrames = 0;
var stopwatch = Stopwatch.StartNew();
var autoCloseSeconds = int.TryParse(Environment.GetEnvironmentVariable("STORYBREW_SPIKE_AUTO_CLOSE_SECONDS"), out var seconds) ? seconds : 0;

window.Load += (_, _) =>
{
    Console.WriteLine($"OpenGL vendor: {GL.GetString(StringName.Vendor)}");
    Console.WriteLine($"OpenGL renderer: {GL.GetString(StringName.Renderer)}");
    Console.WriteLine($"OpenGL version: {GL.GetString(StringName.Version)}");
    Console.WriteLine($"GLSL version: {GL.GetString(StringName.ShadingLanguageVersion)}");
    Console.WriteLine("Window backend: OpenTK 2 GameWindow");
    GL.ClearColor(0.055f, 0.07f, 0.10f, 1f);
};
window.Resize += (_, _) =>
{
    GL.Viewport(0, 0, window.Width, window.Height);
    Console.WriteLine($"Resize: {window.Width}x{window.Height}");
};
window.KeyDown += (_, e) =>
{
    inputSeen = true;
    Console.WriteLine($"KeyDown: {e.Key}");
    if (e.Key == Key.Escape) window.Close();
};
window.MouseDown += (_, e) => { inputSeen = true; Console.WriteLine($"MouseDown: {e.Button}"); };
window.MouseWheel += (_, e) => { inputSeen = true; Console.WriteLine($"MouseWheel: {e.DeltaPrecise}"); };
window.UpdateFrame += (_, _) =>
{
    if (autoCloseSeconds > 0 && stopwatch.Elapsed.TotalSeconds >= autoCloseSeconds) window.Close();
};
window.RenderFrame += (_, _) =>
{
    GL.Clear(ClearBufferMask.ColorBufferBit);
    GL.Begin(PrimitiveType.Triangles);
    GL.Color3(0.95f, 0.35f, 0.28f); GL.Vertex2(0f, 0.65f);
    GL.Color3(0.24f, 0.75f, 0.95f); GL.Vertex2(-0.65f, -0.55f);
    GL.Color3(0.45f, 0.95f, 0.45f); GL.Vertex2(0.65f, -0.55f);
    GL.End();
    window.SwapBuffers();
    renderedFrames++;
};
window.Closed += (_, _) => Console.WriteLine($"Closed cleanly after {renderedFrames} frames; input observed: {inputSeen}");

window.Run(60);

static IntPtr ResolveOpenTkNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
{
    if (libraryName is "opengl32.dll" or "OpenGL")
        return NativeLibrary.Load("/System/Library/Frameworks/OpenGL.framework/OpenGL");

    if (libraryName.Contains("SDL2", StringComparison.OrdinalIgnoreCase))
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "libSDL2-2.0.0.dylib"),
            "/opt/homebrew/lib/libSDL2-2.0.0.dylib",
            "/opt/homebrew/lib/libSDL2.dylib",
            "/usr/local/lib/libSDL2-2.0.0.dylib",
            "/usr/local/lib/libSDL2.dylib",
        ];
        foreach (var candidate in candidates)
            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle)) return handle;
    }

    return IntPtr.Zero;
}
