using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace StorageIntegrationTests;

public static class TestImageHelper
{
    public static byte[] CreateTestJpegImage(int width, int height, int quality = 85)
    {
        using var image = new Image<Rgba32>(width, height);
        
        // Create a colorful test pattern
        image.Mutate(ctx =>
        {
            // Fill with gradient background
            ctx.Fill(Color.LightBlue);
            
            // Add some geometric shapes for visual verification
            ctx.Fill(Color.Red, new RectangleF(10, 10, width / 4, height / 4));
            ctx.Fill(Color.Green, new RectangleF(width - width / 4 - 10, 10, width / 4, height / 4));
            ctx.Fill(Color.Blue, new RectangleF(10, height - height / 4 - 10, width / 4, height / 4));
            ctx.Fill(Color.Yellow, new RectangleF(width - width / 4 - 10, height - height / 4 - 10, width / 4, height / 4));
            
            // Add center circle
            var centerX = width / 2;
            var centerY = height / 2;
            var radius = Math.Min(width, height) / 6;
            ctx.Fill(Color.Purple, new EllipsePolygon(centerX, centerY, radius));
        });

        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream, new JpegEncoder { Quality = quality });
        return stream.ToArray();
    }

    public static byte[] CreateTestPngImage(int width, int height, bool withTransparency = false)
    {
        using var image = new Image<Rgba32>(width, height);
        
        image.Mutate(ctx =>
        {
            if (withTransparency)
            {
                // Create a checkered pattern with transparency
                for (int x = 0; x < width; x += 20)
                {
                    for (int y = 0; y < height; y += 20)
                    {
                        var color = ((x / 20) + (y / 20)) % 2 == 0 
                            ? Color.FromRgba(255, 0, 0, 128) // Semi-transparent red
                            : Color.Transparent;
                        ctx.Fill(color, new RectangleF(x, y, 20, 20));
                    }
                }
            }
            else
            {
                ctx.Fill(Color.Green);
                ctx.Fill(Color.White, new RectangleF(width / 4, height / 4, width / 2, height / 2));
            }
        });

        using var stream = new MemoryStream();
        image.SaveAsPng(stream, new PngEncoder());
        return stream.ToArray();
    }

    public static byte[] CreateLargeTestImage(int width = 4000, int height = 3000)
    {
        using var image = new Image<Rgba32>(width, height);
        
        image.Mutate(ctx =>
        {
            // Create a detailed pattern for compression testing
            ctx.Fill(Color.White);
            
            // Add many small rectangles to test compression
            var random = new Random(12345); // Fixed seed for reproducible results
            for (int i = 0; i < 1000; i++)
            {
                var x = random.Next(0, width - 50);
                var y = random.Next(0, height - 50);
                var w = random.Next(10, 50);
                var h = random.Next(10, 50);
                var color = Color.FromRgb((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
                
                ctx.Fill(color, new RectangleF(x, y, w, h));
            }
        });

        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream, new JpegEncoder { Quality = 95 });
        return stream.ToArray();
    }

    public static byte[] CreateClipboardStyleScreenshot(int width = 1920, int height = 1080)
    {
        using var image = new Image<Rgba32>(width, height);
        
        image.Mutate(ctx =>
        {
            // Simulate a typical screenshot with UI elements
            ctx.Fill(Color.FromRgb(240, 240, 240)); // Light gray background
            
            // Top bar (like a title bar)
            ctx.Fill(Color.FromRgb(0, 120, 215), new RectangleF(0, 0, width, 40));
            
            // Side panel
            ctx.Fill(Color.FromRgb(250, 250, 250), new RectangleF(0, 40, 200, height - 40));
            
            // Main content area
            ctx.Fill(Color.White, new RectangleF(200, 40, width - 200, height - 40));
            
            // Some "text" areas (rectangles representing text)
            for (int i = 0; i < 20; i++)
            {
                var y = 60 + (i * 25);
                ctx.Fill(Color.FromRgb(100, 100, 100), new RectangleF(220, y, 300 + (i % 3) * 50, 15));
            }
            
            // Some "buttons"
            ctx.Fill(Color.FromRgb(0, 120, 215), new RectangleF(220, height - 100, 80, 30));
            ctx.Fill(Color.FromRgb(200, 200, 200), new RectangleF(320, height - 100, 80, 30));
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

    public static TestImageSet CreateTestImageSet()
    {
        return new TestImageSet
        {
            SmallJpeg = CreateTestJpegImage(200, 150),
            MediumJpeg = CreateTestJpegImage(800, 600),
            LargeJpeg = CreateTestJpegImage(1920, 1080),
            PngWithTransparency = CreateTestPngImage(400, 300, withTransparency: true),
            PngWithoutTransparency = CreateTestPngImage(400, 300, withTransparency: false),
            ScreenshotStyle = CreateClipboardStyleScreenshot(),
            VeryLargeImage = CreateLargeTestImage(),
            InvalidData = CreateInvalidImageData()
        };
    }
}

public class TestImageSet
{
    public byte[] SmallJpeg { get; set; } = [];
    public byte[] MediumJpeg { get; set; } = [];
    public byte[] LargeJpeg { get; set; } = [];
    public byte[] PngWithTransparency { get; set; } = [];
    public byte[] PngWithoutTransparency { get; set; } = [];
    public byte[] ScreenshotStyle { get; set; } = [];
    public byte[] VeryLargeImage { get; set; } = [];
    public byte[] InvalidData { get; set; } = [];
}