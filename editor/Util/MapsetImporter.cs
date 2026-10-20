using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace StorybrewEditor.Util
{
    public static class MapsetImporter
    {
        public static bool IsArchive(string path)
            => string.Equals(Path.GetExtension(path), ".osz", StringComparison.OrdinalIgnoreCase);

        // Each import gets a new directory so reimporting never overwrites storyboard edits.
        public static string Import(string archivePath, string projectDirectory)
        {
            using var archive = ZipFile.OpenRead(archivePath);
            if (!archive.Entries.Any(e => !e.FullName.Contains('/') && !e.FullName.Contains('\\')
                && e.FullName.EndsWith(".osu", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("No .osu beatmaps found. In lazer, export For compatibility (.osz).");

            var destination = Path.GetFullPath(Path.Combine(projectDirectory, "mapsets", Guid.NewGuid().ToString("N")));
            var prefix = destination + Path.DirectorySeparatorChar;
            // Validate all entries before creating any files, including Windows-style ZIP paths.
            foreach (var entry in archive.Entries)
            {
                var relative = entry.FullName.Replace('\\', '/');
                var target = Path.GetFullPath(Path.Combine(destination, relative));
                if (!target.StartsWith(prefix, StringComparison.Ordinal) || relative.Contains(':')
                    || ((entry.ExternalAttributes >> 16) & 0xf000) == 0xa000)
                    throw new InvalidDataException("The map archive contains an invalid file path.");
            }
            try
            {
                Directory.CreateDirectory(destination);
                foreach (var entry in archive.Entries)
                {
                    var relative = entry.FullName.Replace('\\', '/');
                    var target = Path.Combine(destination, relative);
                    if (relative.EndsWith('/')) Directory.CreateDirectory(target);
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                        entry.ExtractToFile(target);
                    }
                }
                return destination;
            }
            catch
            {
                if (Directory.Exists(destination)) Directory.Delete(destination, true);
                throw;
            }
        }
    }
}
