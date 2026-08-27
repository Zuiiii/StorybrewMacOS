using Microsoft.Win32;
using System;
using System.IO;

namespace StorybrewEditor.Util
{
    public static class OsuHelper
    {
        public static string GetOsuExePath()
        {
            if (!OperatingSystem.IsWindows()) return string.Empty;
            try
            {
                using (var registryKey = Registry.ClassesRoot.OpenSubKey("osu\\DefaultIcon"))
                    if (registryKey != null)
                    {
                        var value = registryKey.GetValue(null).ToString();
                        var startIndex = value.IndexOf("\"");
                        var endIndex = value.LastIndexOf("\"");
                        return value.Substring(startIndex + 1, endIndex - 1);
                    }
            }
            catch
            {
                // ArgumentOutOfRangeException can happen here from "registryKey.GetValue(null).ToString()"
            }

            // Default stable install path
            var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!", "osu!.exe");
            if (File.Exists(defaultPath))
                return defaultPath;

            return string.Empty;
        }

        public static string GetOsuFolder()
        {
            var osuPath = GetOsuExePath();
            if (string.IsNullOrEmpty(osuPath))
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return Path.GetDirectoryName(osuPath);
        }

        public static string GetOsuSongFolder()
        {
            var osuPath = GetOsuExePath();
            if (string.IsNullOrEmpty(osuPath))
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var osuFolder = Path.GetDirectoryName(osuPath);
            var songsFolder = Path.Combine(osuFolder, "Songs");
            return Directory.Exists(songsFolder) ? songsFolder : osuFolder;
        }
    }
}
