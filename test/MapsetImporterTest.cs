using System.IO.Compression;
using StorybrewEditor.Util;

namespace Test;

[TestClass]
public class MapsetImporterTest
{
    private string root = null!;

    [TestInitialize]
    public void Setup() => Directory.CreateDirectory(root = Path.Combine(Path.GetTempPath(), "storybrew-import-" + Guid.NewGuid()));

    [TestCleanup]
    public void Cleanup() => Directory.Delete(root, true);

    private string Archive(params string[] entries)
    {
        var path = Path.Combine(root, Guid.NewGuid() + ".osz");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var entry in entries)
        {
            using var writer = new StreamWriter(zip.CreateEntry(entry).Open());
            writer.Write(entry.EndsWith(".osu") ? "osu file format v14" : "asset contents");
        }
        return path;
    }

    [TestMethod]
    public void ImportedCopySurvivesDeletingExportAndPreservesAssets()
    {
        var source = Archive("Expert.osu", "audio.mp3", "sb\\background.png");
        var imported = MapsetImporter.Import(source, Path.Combine(root, "project"));
        File.Delete(source);
        Assert.AreEqual("osu file format v14", File.ReadAllText(Path.Combine(imported, "Expert.osu")));
        Assert.AreEqual("asset contents", File.ReadAllText(Path.Combine(imported, "sb", "background.png")));
        Assert.IsTrue(File.Exists(Path.Combine(imported, "audio.mp3")));
    }

    [TestMethod]
    public void ReimportDoesNotOverwriteWorkingCopy()
    {
        var source = Archive("Expert.osu");
        var first = MapsetImporter.Import(source, root);
        File.WriteAllText(Path.Combine(first, "storyboard.osb"), "my edits");
        var second = MapsetImporter.Import(source, root);
        Assert.AreNotEqual(first, second);
        Assert.AreEqual("my edits", File.ReadAllText(Path.Combine(first, "storyboard.osb")));
    }

    [DataTestMethod]
    [DataRow("../escape.txt")]
    [DataRow("..\\escape.txt")]
    [DataRow("/absolute.txt")]
    [DataRow("C:\\escape.txt")]
    public void RejectsPathsOutsideMapset(string entry)
    {
        Assert.ThrowsException<InvalidDataException>(() => MapsetImporter.Import(Archive("Expert.osu", entry), root));
        Assert.IsFalse(Directory.Exists(Path.Combine(root, "mapsets")));
    }

    [TestMethod]
    public void RejectsNonCompatibilityExport()
    {
        Assert.ThrowsException<InvalidDataException>(() => MapsetImporter.Import(Archive("beatmap.json"), root));
    }

    [TestMethod]
    public void FailedExtractionRemovesPartialCopy()
    {
        Assert.ThrowsException<IOException>(() => MapsetImporter.Import(Archive("Expert.osu", "audio.mp3", "audio.mp3"), root));
        Assert.AreEqual(0, Directory.GetDirectories(Path.Combine(root, "mapsets")).Length);
    }
}
