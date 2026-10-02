using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>
    /// Responsive &lt;img&gt; tags for the photos in Images/site, which tools/optimize_images.py writes as
    /// &lt;use&gt;-&lt;width&gt;.webp. The srcset lists whatever widths exist on disk, so pages name a use
    /// ("hero", "campus-bisho") and never a file or a size.
    /// </summary>
    public static class SiteImage
    {
        private static readonly ConcurrentDictionary<string, int[]> Widths = new ConcurrentDictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);

        public static string Img(string use, string alt, string sizes = "100vw", bool eager = false, string css = null)
        {
            var widths = WidthsOf(use);
            if (widths.Length == 0) return "";
            var srcset = string.Join(", ", widths.Select(w => FileUrl(use, w) + " " + w + "w"));
            return "<img src=\"" + FileUrl(use, widths.Max()) + "\" srcset=\"" + srcset + "\" sizes=\"" + sizes + "\" alt=\"" +
                   HttpUtility.HtmlAttributeEncode(alt) + "\"" + (css == null ? "" : " class=\"" + css + "\"") +
                   (eager ? " fetchpriority=\"high\"" : " loading=\"lazy\"") + " decoding=\"async\" />";
        }

        /// <summary>The largest file for a use, for CSS backgrounds.</summary>
        public static string Url(string use)
        {
            var widths = WidthsOf(use);
            return widths.Length == 0 ? "" : FileUrl(use, widths.Max());
        }

        private static string FileUrl(string use, int width) => VirtualPathUtility.ToAbsolute("~/Images/site/" + use + "-" + width + ".webp");

        private static int[] WidthsOf(string use) => Widths.GetOrAdd(use, u =>
        {
            var folder = HostingEnvironment.MapPath("~/Images/site");
            if (folder == null || !Directory.Exists(folder)) return new int[0];
            return Directory.GetFiles(folder, u + "-*.webp")
                .Select(f => Path.GetFileNameWithoutExtension(f).Substring(u.Length + 1))
                .Select(s => int.TryParse(s, out var w) ? w : 0)
                .Where(w => w > 0).OrderBy(w => w).ToArray();
        });
    }
}
