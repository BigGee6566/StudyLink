using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace StudyLink.Services.Validation
{
    /// <summary>
    /// Server-side rules ported from TutorConnect UserValidator. Client-side validation is a
    /// convenience only; these run on every submission regardless.
    /// </summary>
    public static class UserValidator
    {
        // Same patterns as TutorConnect so existing data and habits carry over.
        private static readonly Regex EmailPattern = new Regex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.Compiled);
        private static readonly Regex CellPattern = new Regex(@"^(0\d{9}|\+27\d{9})$", RegexOptions.Compiled);
        private static readonly Regex UserNamePattern = new Regex(@"^[A-Za-z0-9._-]{3,50}$", RegexOptions.Compiled);
        private static readonly Regex StudentNumberPattern = new Regex(@"^S\d{9}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>Student numbers are 10 characters: the letter S and 9 digits (stored upper-case).</summary>
        public const string StudentNumberMessage = "Enter your student number: the letter S followed by 9 digits (for example S202600001).";

        /// <summary>The same rule for the browser's pattern attribute, so the form says so before posting.</summary>
        public const string StudentNumberHtmlPattern = "[Ss][0-9]{9}";

        public const int MinPasswordLength = 8;

        public static string NormaliseCell(string cell) => (cell ?? string.Empty).Replace(" ", string.Empty).Trim();

        public static bool IsValidEmail(string email) =>
            !string.IsNullOrWhiteSpace(email) && email.Length <= 100 && EmailPattern.IsMatch(email.Trim());

        public static bool IsValidCell(string cell) => CellPattern.IsMatch(NormaliseCell(cell));

        public static bool IsValidUserName(string userName) =>
            !string.IsNullOrWhiteSpace(userName) && UserNamePattern.IsMatch(userName.Trim());

        public static bool IsValidStudentNumber(string number) =>
            !string.IsNullOrWhiteSpace(number) && StudentNumberPattern.IsMatch(number.Trim());

        /// <summary>BR-02/03: at least 8 characters, with a letter and a digit, matching the confirmation.</summary>
        public static string CheckPassword(string password, string confirm)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
                return "Password must be at least 8 characters long.";
            if (password.Length > 128)
                return "Password must be 128 characters or fewer.";
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
                return "Password must contain at least one letter and one number.";
            if (confirm != null && !string.Equals(password, confirm, StringComparison.Ordinal))
                return "The passwords do not match.";
            return null;
        }

        /// <summary>Validates the personal details every account shares. Returns the first problem, or null.</summary>
        public static string CheckPersonalDetails(string userName, string firstName, string lastName, string email, string cell)
        {
            var errors = new List<string>();
            if (!IsValidUserName(userName))
                return "Username must be 3–50 characters: letters, numbers, dot, dash or underscore.";
            if (string.IsNullOrWhiteSpace(firstName) || firstName.Trim().Length > 50)
                return "First name is required (50 characters max).";
            if (string.IsNullOrWhiteSpace(lastName) || lastName.Trim().Length > 50)
                return "Last name is required (50 characters max).";
            if (!IsValidEmail(email))
                return "Enter a valid email address.";
            if (!IsValidCell(cell))
                return "Enter a South African cell number, e.g. 0821234567 or +27821234567.";
            return null;
        }
    }

    public static class TextRules
    {
        /// <summary>Trims and returns null for empty input, so optional fields store NULL not "".</summary>
        public static string Clean(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var trimmed = value.Trim();
            return trimmed.Length > maxLength ? trimmed.Substring(0, maxLength) : trimmed;
        }

        public static bool TooLong(string value, int maxLength) => value != null && value.Trim().Length > maxLength;
    }

    /// <summary>
    /// Uploaded images (book covers, profile photos) are identified by their first bytes, never by the
    /// file name or the type the browser claims, so a renamed script or HTML file cannot be stored.
    /// </summary>
    public static class ImageRules
    {
        /// <summary>Largest picture a user may send. Phone photos are larger than what is kept, so they are shrunk.</summary>
        public const int MaxUploadBytes = 15 * 1024 * 1024;

        /// <summary>Largest picture kept in the database.</summary>
        public const int MaxBytes = 2 * 1024 * 1024;

        public const int AvatarSide = 512;
        public const int CoverSide = 900;

        // A small file can still decode to a huge bitmap; refuse those before allocating it.
        private const long MaxPixels = 60000000;
        private const int ExifOrientation = 0x0112;

        /// <summary>"image/jpeg", "image/png" or "image/webp"; null for anything else.</summary>
        public static string Detect(byte[] data)
        {
            if (data == null || data.Length < 12) return null;
            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) return "image/jpeg";
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
                && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A) return "image/png";
            if (data[0] == (byte)'R' && data[1] == (byte)'I' && data[2] == (byte)'F' && data[3] == (byte)'F'
                && data[8] == (byte)'W' && data[9] == (byte)'E' && data[10] == (byte)'B' && data[11] == (byte)'P') return "image/webp";
            return null;
        }

        /// <summary>
        /// Checks an uploaded picture and returns what to store. JPEG photos are always redrawn: upright,
        /// at most <paramref name="maxSide"/> pixels a side, without camera metadata (phones embed the GPS
        /// position). A PNG that is already small enough is kept as sent. WebP cannot be decoded on .NET
        /// Framework, so it is kept as sent and must already fit within <see cref="MaxBytes"/>.
        /// </summary>
        public static PreparedImage Prepare(byte[] data, int maxSide)
        {
            if (data == null || data.Length == 0) return PreparedImage.Fail("Choose an image to upload.");
            if (data.Length > MaxUploadBytes) return PreparedImage.Fail("The picture must be 15 MB or smaller.");
            var type = Detect(data);
            if (type == null) return PreparedImage.Fail("The image must be a JPEG, PNG or WebP picture.");
            if (type == "image/webp")
                return data.Length <= MaxBytes
                    ? new PreparedImage { Data = data, ContentType = type }
                    : PreparedImage.Fail("WebP pictures must be 2 MB or smaller. Save the picture as a JPEG to upload a larger one.");

            try
            {
                using (var input = new MemoryStream(data))
                using (var image = Image.FromStream(input, false, true))
                {
                    if ((long)image.Width * image.Height > MaxPixels)
                        return PreparedImage.Fail("The picture has too many pixels. Use a smaller photo.");
                    if (type == "image/png" && data.Length <= MaxBytes && image.Width <= maxSide && image.Height <= maxSide)
                        return new PreparedImage { Data = data, ContentType = type };

                    var jpeg = Redraw(image, maxSide);
                    return jpeg.Length <= MaxBytes
                        ? new PreparedImage { Data = jpeg, ContentType = "image/jpeg" }
                        : PreparedImage.Fail("The picture is too large even after shrinking it. Use a smaller photo.");
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is ExternalException || ex is OutOfMemoryException)
            {
                // GDI+ reports a damaged or unsupported file as one of these.
                return PreparedImage.Fail("This picture could not be read. Try another photo.");
            }
        }

        private static byte[] Redraw(Image image, int maxSide)
        {
            ApplyOrientation(image);
            var scale = Math.Min(1.0, (double)maxSide / Math.Max(image.Width, image.Height));
            var width = Math.Max(1, (int)Math.Round(image.Width * scale));
            var height = Math.Max(1, (int)Math.Round(image.Height * scale));

            using (var canvas = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            {
                using (var g = Graphics.FromImage(canvas))
                {
                    g.Clear(Color.White); // transparent PNG areas become white in a JPEG
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(image, 0, 0, width, height);
                }

                var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                using (var quality = new EncoderParameters(1))
                using (var output = new MemoryStream())
                {
                    quality.Param[0] = new EncoderParameter(Encoder.Quality, 85L);
                    canvas.Save(output, codec, quality);
                    return output.ToArray();
                }
            }
        }

        /// <summary>Phones store "rotate me" in EXIF instead of rotating the pixels; browsers would show the raw pixels.</summary>
        private static void ApplyOrientation(Image image)
        {
            if (!image.PropertyIdList.Contains(ExifOrientation)) return;
            var value = image.GetPropertyItem(ExifOrientation).Value;
            if (value == null || value.Length == 0) return;
            switch (value[0])
            {
                case 2: image.RotateFlip(RotateFlipType.RotateNoneFlipX); break;
                case 3: image.RotateFlip(RotateFlipType.Rotate180FlipNone); break;
                case 4: image.RotateFlip(RotateFlipType.Rotate180FlipX); break;
                case 5: image.RotateFlip(RotateFlipType.Rotate90FlipX); break;
                case 6: image.RotateFlip(RotateFlipType.Rotate90FlipNone); break;
                case 7: image.RotateFlip(RotateFlipType.Rotate270FlipX); break;
                case 8: image.RotateFlip(RotateFlipType.Rotate270FlipNone); break;
            }
        }
    }

    /// <summary>The result of <see cref="ImageRules.Prepare"/>: the bytes to store and their type, or why not.</summary>
    public sealed class PreparedImage
    {
        public byte[] Data { get; set; }
        public string ContentType { get; set; }
        public string Error { get; private set; }

        internal static PreparedImage Fail(string error) => new PreparedImage { Error = error };
    }
}
