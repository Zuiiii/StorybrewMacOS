using BrewLib.Graphics.Textures;
using SkiaSharp;
using System.Runtime.InteropServices;
namespace Test;

[TestClass]
public class TextureUploadTest
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void UploadConvertsRgbaToBgraAndHonoursAlphaOption(bool premultiply)
    {
        using var source = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        source.SetPixel(0, 0, new SKColor(200, 80, 40, 128));
        using var upload = Texture2d.PrepareSkiaUpload(source, new TextureOptions { PreMultiply = premultiply });
        var data = new byte[8];
        Marshal.Copy(upload.GetPixels(), data, 0, data.Length);
        Assert.AreEqual(premultiply ? 20 : 40, (int)data[0]);
        Assert.AreEqual(premultiply ? 40 : 80, (int)data[1]);
        Assert.AreEqual(premultiply ? 100 : 200, (int)data[2]);
        Assert.AreEqual(128, (int)data[3]);
    }

    [TestMethod]
    public void CroppedUploadUsesPackedRows()
    {
        using var source = new SKBitmap(4, 2);
        source.SetPixel(0, 1, SKColors.Red);
        using var upload = Texture2d.PrepareSkiaUpload(source, TextureOptions.Default, 2, 2);
        Assert.AreEqual(8, upload.RowBytes);
        Assert.AreEqual(SKColors.Red, upload.GetPixel(0, 1));
    }
}
