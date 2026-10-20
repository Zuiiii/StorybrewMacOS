using OpenTK.Graphics;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Drawing2D;
using StorybrewCommon.Util;

namespace StorybrewCommon.Subtitles
{
    public partial class FontGenerator
    {
        private static SKColor skiaColor(Color4 color) => new SKColor(unchecked((uint)color.ToArgb()));

        private FontTexture generateTextureSkia(string text)
        {
            var filename = text.Length == 1 ? $"{(int)text[0]:x4}.png" : $"_{textureCache.Count(l => l.Key.Length > 1):x3}.png";
            var bitmapPath = Path.Combine(assetDirectory, Directory, filename);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(bitmapPath));
            var fontPath = Path.Combine(projectDirectory, description.FontPath);
            var style = new SKFontStyle(description.FontStyle.HasFlag(FontStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal, description.FontStyle.HasFlag(FontStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
            using var typeface = File.Exists(fontPath) ? SKTypeface.FromFile(fontPath) : SKTypeface.FromFamilyName(description.FontPath, style);
            if (typeface == null) throw new InvalidOperationException($"Cannot load font '{description.FontPath}'.");
            // FontSize is in points, matching GDI's 96-dpi layout.
            using var font = new SKFont(typeface, description.FontSize * 96f / 72f)
            {
                Edging = SKFontEdging.Antialias,
                Embolden = description.FontStyle.HasFlag(FontStyle.Bold) && typeface.FontWeight < (int)SKFontStyleWeight.Bold,
                SkewX = description.FontStyle.HasFlag(FontStyle.Italic) && typeface.FontSlant == SKFontStyleSlant.Upright ? -0.25f : 0,
            };
            using var paint = new SKPaint { IsAntialias = true, Color = skiaColor(description.Color) };
            var lines = text.Replace("\r", "").Split('\n');
            var metrics = font.Metrics;
            var lineHeight = metrics.Descent - metrics.Ascent + metrics.Leading;
            float advance = 0, minLeft = 0, maxRight = 0;
            foreach (var line in lines)
            {
                advance = Math.Max(advance, font.MeasureText(line, out var bounds, paint));
                minLeft = Math.Min(minLeft, bounds.Left);
                maxRight = Math.Max(maxRight, bounds.Right);
            }
            var baseWidth = (int)Math.Ceiling(advance);
            var baseHeight = text.Length == 0 ? 0 : (int)Math.Ceiling(lineHeight * lines.Length);
            float effectsWidth = 0, effectsHeight = 0;
            foreach (var effect in effects)
            {
                var size = effect.Measure();
                effectsWidth = Math.Max(effectsWidth, size.X);
                effectsHeight = Math.Max(effectsHeight, size.Y);
            }
            var paddingX = description.Padding.X + effectsWidth * 0.5f;
            var paddingY = description.Padding.Y + effectsHeight * 0.5f;
            var left = paddingX - minLeft;
            var width = (int)Math.Ceiling(baseWidth + paddingX * 2 - minLeft + Math.Max(0, maxRight - baseWidth));
            var height = (int)Math.Ceiling(baseHeight + paddingY * 2);
            float offsetX = -left, offsetY = -paddingY;
            if (string.IsNullOrWhiteSpace(text) || width <= 0 || height <= 0)
                return new FontTexture(null, offsetX, offsetY, baseWidth, baseHeight, Math.Max(0, width), Math.Max(0, height));

            using var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);
                if (description.Debug) canvas.Clear(new SKColor(100, 100, 100));
                void draw(SKPaint brush, float dx = 0, float dy = 0, SKCanvas target = null)
                {
                    target ??= canvas;
                    for (var i = 0; i < lines.Length; i++)
                    {
                        var baseline = paddingY - metrics.Ascent + i * lineHeight + dy;
                        var length = font.MeasureText(lines[i], brush);
                        var lineX = left + (advance - length) * 0.5f + dx;
                        target.DrawText(lines[i], lineX, baseline, SKTextAlign.Left, font, brush);
                        if (description.FontStyle.HasFlag(FontStyle.Underline))
                            target.DrawRect(lineX, baseline + metrics.Descent * 0.5f, length, Math.Max(1, font.Size / 16), brush);
                        if (description.FontStyle.HasFlag(FontStyle.Strikeout))
                            target.DrawRect(lineX, baseline + metrics.Ascent * 0.35f, length, Math.Max(1, font.Size / 16), brush);
                    }
                }
                void drawEffect(FontEffect effect)
                {
                    using var brush = new SKPaint { IsAntialias = true };
                    switch (effect)
                    {
                        case FontBackground background:
                            canvas.Clear(skiaColor(background.Color));
                            break;
                        case FontOutline outline:
                            brush.Color = skiaColor(outline.Color);
                            for (var i = 1; i <= outline.Thickness; i++)
                                if (i % 2 == 0)
                                {
                                    var d = i * MathF.Sqrt(2);
                                    draw(brush, -d); draw(brush, d); draw(brush, 0, -d); draw(brush, 0, d);
                                }
                                else
                                {
                                    draw(brush, -i, -i); draw(brush, i, -i); draw(brush, -i, i); draw(brush, i, i);
                                }
                            break;
                        case FontShadow shadow:
                            brush.Color = skiaColor(shadow.Color);
                            for (var i = 1; i <= shadow.Thickness; i++) draw(brush, i, i);
                            break;
                        case FontGlow glow:
                            if (glow.Radius < 1) break;
                            // Match FontGlow's finite, normalized convolution (including its
                            // radius cap and alpha semantics), rather than Skia's blur filter.
                            using (var source = new SKBitmap(width, height))
                            using (var sourceCanvas = new SKCanvas(source))
                            {
                                sourceCanvas.Clear(SKColors.Transparent);
                                brush.Color = SKColors.White;
                                draw(brush, target: sourceCanvas);
                                sourceCanvas.Flush();
                                using var blurred = ConvoluteGlow(source, glow);
                                canvas.DrawBitmap(blurred, 0, 0);
                            }
                            break;
                        case FontGradient gradient:
                            var color = skiaColor(gradient.Color);
                            var start = new SKPoint(left + advance / 2 + gradient.Offset.X, paddingY + gradient.Offset.Y);
                            var end = new SKPoint(start.X + gradient.Size.X, start.Y + gradient.Size.Y);
                            var tile = gradient.WrapMode == WrapMode.Clamp ? SKShaderTileMode.Clamp
                                : gradient.WrapMode == WrapMode.Tile ? SKShaderTileMode.Repeat : SKShaderTileMode.Mirror;
                            using (var shader = SKShader.CreateLinearGradient(start, end, new[] { color, color.WithAlpha(0) }, null, tile))
                            {
                                brush.Shader = shader;
                                draw(brush);
                                brush.Shader = null;
                            }
                            break;
                        default:
                            throw new NotSupportedException($"Font effect '{effect.GetType().Name}' needs a cross-platform renderer.");
                    }
                }
                foreach (var effect in effects) if (!effect.Overlay) drawEffect(effect);
                if (!description.EffectsOnly) draw(paint);
                foreach (var effect in effects) if (effect.Overlay) drawEffect(effect);
            }
            var boundsToSave = new SKRectI(0, 0, width, height);
            if (description.TrimTransparency)
            {
                var x0 = width; var y0 = height; var x1 = -1; var y1 = -1;
                for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++)
                        if (bitmap.GetPixel(x, y).Alpha > 0)
                        { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
                if (x1 < 0) return new FontTexture(null, offsetX, offsetY, baseWidth, baseHeight, 0, 0);
                boundsToSave = new SKRectI(Math.Max(0, x0 - 1), Math.Max(0, y0 - 1), Math.Min(width, x1 + 2), Math.Min(height, y1 + 2));
                offsetX += boundsToSave.Left; offsetY += boundsToSave.Top;
            }
            using var cropped = new SKBitmap();
            if (!bitmap.ExtractSubset(cropped, boundsToSave)) throw new InvalidOperationException("Cannot crop font texture.");
            using var image = SKImage.FromBitmap(cropped);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            BrewLib.Util.Misc.WithRetries(() => { using var output = File.Create(bitmapPath); data.SaveTo(output); });
            return new FontTexture(Path.Combine(Directory, filename), offsetX, offsetY, baseWidth, baseHeight, cropped.Width, cropped.Height);
        }
        internal static SKBitmap ConvoluteGlow(SKBitmap source, FontGlow glow)
        {
            var radius = Math.Min(glow.Radius, 24);
            var kernel = BitmapHelper.CalculateGaussianKernel(radius, glow.Power >= 1 ? glow.Power : glow.Radius * 0.5);
            var pixels = source.Pixels;
            var result = new SKBitmap(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            var output = new SKColor[pixels.Length];
            var color = skiaColor(glow.Color);
            for (var y = 0; y < source.Height; y++)
                for (var x = 0; x < source.Width; x++)
                {
                    double alpha = 0;
                    for (var kx = -radius; kx <= radius; kx++)
                        for (var ky = -radius; ky <= radius; ky++)
                            alpha += pixels[Math.Clamp(y + ky, 0, source.Height - 1) * source.Width
                                + Math.Clamp(x + kx, 0, source.Width - 1)].Alpha * kernel[ky + radius, kx + radius];
                    // Upstream ConvoluteAlpha uses the supplied RGB but replaces its alpha.
                    output[y * source.Width + x] = color.WithAlpha((byte)Math.Clamp((int)alpha, 0, 255));
                }
            result.Pixels = output;
            return result;
        }
    }
}
