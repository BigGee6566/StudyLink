using System;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using StudyLink.Data;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>
    /// Textbook covers shipped with the site (Images/covers/&lt;isbn&gt;.jpg, from tools/fetch_covers.py).
    /// At start-up, a book that has no cover yet gets the shipped one for its ISBN. A manager's upload
    /// is never replaced, and books with neither show the placeholder jacket.
    /// </summary>
    public static class CoverLibrary
    {
        public static int FillMissing(StudyLinkDbContext db)
        {
            var folder = HostingEnvironment.MapPath("~/Images/covers");
            if (folder == null || !Directory.Exists(folder)) return 0;
            var files = Directory.GetFiles(folder, "*.jpg").ToDictionary(Path.GetFileNameWithoutExtension, f => f, StringComparer.OrdinalIgnoreCase);
            if (files.Count == 0) return 0;

            var isbns = files.Keys.ToList();
            var books = db.Books.Where(b => b.CoverImage == null && isbns.Contains(b.Isbn)).ToList();
            foreach (var book in books)
            {
                book.CoverImage = File.ReadAllBytes(files[book.Isbn]);
                book.CoverContentType = "image/jpeg";
            }
            if (books.Count > 0) db.SaveChanges();
            return books.Count;
        }
    }
}
