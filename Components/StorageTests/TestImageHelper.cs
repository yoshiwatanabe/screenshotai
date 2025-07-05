using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LocalStorageTests;

public static class TestImageHelper
{
    public static byte[] CreateTestJpegImage(int width, int height, int quality = 85)
    {
        using var image = new Image<Rgba32>(width, height);

        // Create a simple colored image
        image.Mutate(ctx =>
        {
            ctx.BackgroundColor(Color.LightBlue);
        });

        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream, new JpegEncoder { Quality = quality });
        return stream.ToArray();
    }

    public static byte[] CreateScreenshotStyleImage(int width = 1920, int height = 1080)
    {
        using var image = new Image<Rgba32>(width, height);

        image.Mutate(ctx =>
        {
            ctx.BackgroundColor(Color.White);
        });

        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream, new JpegEncoder { Quality = 90 });
        return stream.ToArray();
    }

    public static byte[] CreateInvalidImageData()
    {
        // Return some bytes that look like they might be an image but aren't
        var invalidData = new byte[1000];
        var random = new Random(67890);
        random.NextBytes(invalidData);

        // Add some "header-like" bytes to make it more realistic
        invalidData[0] = 0xFF; // JPEG marker start
        invalidData[1] = 0xD8; // JPEG marker
                               // But the rest is garbage

        return invalidData;
    }
}