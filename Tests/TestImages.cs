using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;

namespace StudyLink.Tests
{
    /// <summary>Real, decodable pictures for upload tests, drawn in memory.</summary>
    public static class TestImages
    {
        public static byte[] Png(int width, int height) => Draw(width, height, ImageFormat.Png, false, null);

        /// <summary>
        /// A JPEG. <paramref name="noisy"/> fills it with random pixels so it compresses badly, like a
        /// phone photo; <paramref name="exifOrientation"/> adds the EXIF "rotate me" tag phones write.
        /// </summary>
        public static byte[] Jpeg(int width, int height, bool noisy = false, short? exifOrientation = null) =>
            Draw(width, height, ImageFormat.Jpeg, noisy, exifOrientation);

        public static Size SizeOf(byte[] image)
        {
            using (var ms = new MemoryStream(image))
            using (var decoded = Image.FromStream(ms))
                return decoded.Size;
        }

        public static bool HasExif(byte[] image)
        {
            using (var ms = new MemoryStream(image))
            using (var decoded = Image.FromStream(ms))
                return decoded.PropertyIdList.Length > 0 && Array.IndexOf(decoded.PropertyIdList, 0x0112) >= 0;
        }

        private static byte[] Draw(int width, int height, ImageFormat format, bool noisy, short? orientation)
        {
            using (var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            {
                if (noisy)
                {
                    var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmp.PixelFormat);
                    var bytes = new byte[data.Stride * height];
                    new Random(42).NextBytes(bytes);
                    System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
                    bmp.UnlockBits(data);
                }
                else
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.SteelBlue);
                        g.FillRectangle(Brushes.Gold, 0, 0, width / 2, height / 4);
                    }
                }

                if (orientation.HasValue)
                {
                    // PropertyItem has no public constructor; this is the documented workaround.
                    var item = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
                    item.Id = 0x0112;
                    item.Type = 3;
                    item.Len = 2;
                    item.Value = BitConverter.GetBytes(orientation.Value);
                    bmp.SetPropertyItem(item);
                }

                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, format);
                    return ms.ToArray();
                }
            }
        }
    }
}
