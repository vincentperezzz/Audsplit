using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Audsplit;

internal static class TrayIconFactory
{
    private static readonly int[] Sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256];

    public static string CreateTempIcon()
    {
        var path = Path.Combine(Path.GetTempPath(), "audsplit-tray.ico");
        using var logo = LoadLogo();
        using var cropped = CropToContent(logo);

        var frames = new List<byte[]>(Sizes.Length);
        foreach (var size in Sizes)
        {
            using var frame = RenderExact(cropped, size);
            frames.Add(EncodePng(frame));
        }

        WritePngIco(path, frames, Sizes);
        return path;
    }

    private static Bitmap LoadLogo()
    {
        var uri = new Uri("pack://application:,,,/Assets/audsplit-logo.png");
        using var stream = System.Windows.Application.GetResourceStream(uri)?.Stream
            ?? throw new InvalidOperationException("Missing Assets/audsplit-logo.png");
        using var src = new Bitmap(stream);
        return new Bitmap(src);
    }

    private static Bitmap CropToContent(Bitmap src)
    {
        var w = src.Width;
        var h = src.Height;
        var rect = new Rectangle(0, 0, w, h);
        var data = src.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var buffer = new byte[stride * h];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            var minX = w;
            var minY = h;
            var maxX = -1;
            var maxY = -1;

            for (var y = 0; y < h; y++)
            {
                var row = y * stride;
                for (var x = 0; x < w; x++)
                {
                    var a = buffer[row + (x * 4) + 3];
                    if (a < 16)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return new Bitmap(src);
            }

            var pad = Math.Max(2, Math.Min(w, h) / 128);
            minX = Math.Max(0, minX - pad);
            minY = Math.Max(0, minY - pad);
            maxX = Math.Min(w - 1, maxX + pad);
            maxY = Math.Min(h - 1, maxY + pad);

            var crop = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
            return src.Clone(crop, PixelFormat.Format32bppArgb);
        }
        finally
        {
            src.UnlockBits(data);
        }
    }

    private static Bitmap RenderExact(Bitmap logo, int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.CompositingMode = CompositingMode.SourceOver;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = size <= 24
            ? InterpolationMode.HighQualityBicubic
            : InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var pad = size <= 16 ? 1 : Math.Max(1, size / 32);
        var box = size - (pad * 2);
        var scale = Math.Min((float)box / logo.Width, (float)box / logo.Height);
        var dw = Math.Max(1, (int)Math.Round(logo.Width * scale));
        var dh = Math.Max(1, (int)Math.Round(logo.Height * scale));
        var dx = (size - dw) / 2;
        var dy = (size - dh) / 2;

        g.DrawImage(logo, new Rectangle(dx, dy, dw, dh));
        return bmp;
    }

    private static byte[] EncodePng(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    private static void WritePngIco(string path, IReadOnlyList<byte[]> pngFrames, IReadOnlyList<int> sizes)
    {
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);

        bw.Write((ushort)0);
        bw.Write((ushort)1);
        bw.Write((ushort)pngFrames.Count);

        var offset = 6 + (16 * pngFrames.Count);
        for (var i = 0; i < pngFrames.Count; i++)
        {
            var size = sizes[i];
            bw.Write((byte)(size >= 256 ? 0 : size));
            bw.Write((byte)(size >= 256 ? 0 : size));
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((ushort)1);
            bw.Write((ushort)32);
            bw.Write(pngFrames[i].Length);
            bw.Write(offset);
            offset += pngFrames[i].Length;
        }

        foreach (var frame in pngFrames)
        {
            bw.Write(frame);
        }
    }
}
