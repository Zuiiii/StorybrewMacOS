using System.Reflection;
using OpenTK;
using OpenTK.Graphics;
using SkiaSharp;
using StorybrewCommon.Subtitles;

namespace Test;

[TestClass]
public class FontGeneratorTest
{
    private string root = null!;
    [TestInitialize]
    public void Setup() => Directory.CreateDirectory(root = Path.Combine(Path.GetTempPath(), "storybrew-font-" + Guid.NewGuid()));
    [TestCleanup]
    public void Cleanup() => Directory.Delete(root, true);

    private FontGenerator Font(FontDescription description, params FontEffect[] effects)
        => (FontGenerator)Activator.CreateInstance(typeof(FontGenerator), BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { "font", description, effects, root, root }, null)!;

    [TestMethod]
    public void CreatesVisiblePngAndCachesTexture()
    {
        var font = Font(new FontDescription { FontPath = "Arial", FontSize = 26, Color = Color4.White, TrimTransparency = true });
        var texture = font.GetTexture("BPM 170");
        using var image = SKBitmap.Decode(Path.Combine(root, texture.Path));
        Assert.AreEqual(texture.Width, image.Width);
        Assert.AreEqual(texture.Height, image.Height);
        Assert.IsTrue(image.Pixels.Any(p => p.Alpha > 0));
        Assert.AreSame(texture, font.GetTexture("BPM 170"));
    }

    [TestMethod]
    public void WhitespaceHasAdvanceButNoImage()
    {
        var texture = Font(new FontDescription { FontPath = "Arial", FontSize = 26 }).GetTexture(" ");
        Assert.IsTrue(texture.IsEmpty);
        Assert.IsTrue(texture.BaseWidth > 0);
    }

    [DataTestMethod]
    [DataRow("outline")]
    [DataRow("shadow")]
    [DataRow("glow")]
    [DataRow("gradient")]
    [DataRow("background")]
    public void BuiltInEffectsRenderWithoutGdi(string effectName)
    {
        FontEffect effect = effectName switch
        {
            "outline" => new FontOutline { Thickness = 3, Color = Color4.Red },
            "shadow" => new FontShadow { Thickness = 3, Color = Color4.Red },
            "glow" => new FontGlow { Radius = 6, Color = Color4.Red },
            "gradient" => new FontGradient { Color = Color4.Red },
            _ => new FontBackground { Color = Color4.Red },
        };
        var texture = Font(new FontDescription { FontPath = "Arial", FontSize = 26, EffectsOnly = true, TrimTransparency = true }, effect).GetTexture("Test");
        using var image = SKBitmap.Decode(Path.Combine(root, texture.Path));
        Assert.IsTrue(image.Pixels.Any(p => p.Red > 0 && p.Alpha > 0));
    }

    [TestMethod]
    public void TrimmingKeepsBaseDimensionsAndPosition()
    {
        var plain = Font(new FontDescription { FontPath = "Arial", FontSize = 26, Color = Color4.White, Padding = new Vector2(10) }).GetTexture("j");
        var cropped = Font(new FontDescription { FontPath = "Arial", FontSize = 26, Color = Color4.White, Padding = new Vector2(10), TrimTransparency = true }).GetTexture("j");
        Assert.AreEqual(plain.BaseWidth, cropped.BaseWidth);
        Assert.AreEqual(plain.BaseHeight, cropped.BaseHeight);
        Assert.IsTrue(cropped.Width < plain.Width);
        Assert.IsTrue(cropped.OffsetX > plain.OffsetX);
        Assert.IsTrue(cropped.OffsetY > plain.OffsetY);
    }

    [TestMethod]
    public void TrimRetainsUpstreamOnePixelBorder()
    {
        var texture = Font(new FontDescription { FontPath = "Arial", FontSize = 26, Color = Color4.White,
            Padding = new Vector2(20), TrimTransparency = true }).GetTexture("M");
        using var image = SKBitmap.Decode(Path.Combine(root, texture.Path));
        for (var x = 0; x < image.Width; x++)
        {
            Assert.AreEqual(0, (int)image.GetPixel(x, 0).Alpha);
            Assert.AreEqual(0, (int)image.GetPixel(x, image.Height - 1).Alpha);
        }
        Assert.IsTrue(Enumerable.Range(0, image.Width).Any(x => image.GetPixel(x, 1).Alpha > 0));
    }

    [TestMethod]
    public void GlowUsesUpstreamFiniteKernelAndReplacesColorAlpha()
    {
        using var source = new SKBitmap(7, 7);
        source.Erase(SKColors.Transparent);
        source.SetPixel(3, 3, SKColors.White);
        var method = typeof(FontGenerator).GetMethod("ConvoluteGlow", BindingFlags.NonPublic | BindingFlags.Static)!;
        using var result = (SKBitmap)method.Invoke(null, new object[] { source,
            new FontGlow { Radius = 1, Power = 1, Color = new Color4(255, 0, 0, 0) } })!;
        var kernel = StorybrewCommon.Util.BitmapHelper.CalculateGaussianKernel(1, 1);
        Assert.AreEqual((int)(255 * kernel[1, 1]), (int)result.GetPixel(3, 3).Alpha);
        Assert.AreEqual(0, (int)result.GetPixel(1, 3).Alpha);
        Assert.AreEqual(255, (int)result.GetPixel(3, 3).Red);
    }
}
