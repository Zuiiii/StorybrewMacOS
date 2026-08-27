using System;
using System.Diagnostics;
using System.IO;

namespace StorybrewEditor.Platform
{
    public static class PlatformServices
    {
        public static string PickFolder(string title, string initialDirectory)
            => RunAppleScript($"POSIX path of (choose folder with prompt {Quote(title)}{DefaultLocation(initialDirectory)})");

        public static string PickFile(string title, string initialFile, string initialDirectory)
            => RunAppleScript($"POSIX path of (choose file with prompt {Quote(title)}{DefaultLocation(initialDirectory)})");

        public static string PickSaveLocation(string title, string initialFile)
            => RunAppleScript($"POSIX path of (choose file name with prompt {Quote(title)} default name {Quote(initialFile ?? string.Empty)})");

        public static void ShowError(string title, string message)
        {
            Trace.WriteLine($"{title}: {message}");
            if (OperatingSystem.IsMacOS())
                RunAppleScript($"display alert {Quote(title)} message {Quote(message)} as critical", false);
        }

        public static void Open(string target)
        {
            Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
        }

        private static string DefaultLocation(string path)
            => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path)
                ? $" default location (POSIX file {Quote(Path.GetFullPath(path))})"
                : string.Empty;

        private static string Quote(string value)
            => "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ") + "\"";

        private static string RunAppleScript(string script, bool throwOnFailure = true)
        {
            if (!OperatingSystem.IsMacOS())
                throw new PlatformNotSupportedException("A native dialog implementation is not available for this platform build.");

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/osascript",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                }
            };
            process.StartInfo.ArgumentList.Add("-e");
            process.StartInfo.ArgumentList.Add(script);
            process.Start();
            var output = process.StandardOutput.ReadToEnd().TrimEnd('\r', '\n');
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode == 0) return output;
            // User cancellation is AppleScript error -128.
            if (error.Contains("(-128)", StringComparison.Ordinal)) return null;
            if (throwOnFailure) throw new InvalidOperationException(error.Trim());
            return null;
        }
    }
}
