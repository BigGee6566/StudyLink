using System.IO;
using System.Web;
using System.Web.Hosting;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>
    /// Cache-busting URLs for the site's own CSS and JavaScript. Static files are cached for 7 days
    /// (web.config clientCache), so each URL carries the file's last-write time: a changed file gets a
    /// new URL and browsers fetch it at once.
    /// </summary>
    public static class Asset
    {
        public static string Url(string appRelativePath)
        {
            var file = HostingEnvironment.MapPath(appRelativePath);
            var stamp = file != null && File.Exists(file) ? File.GetLastWriteTimeUtc(file).Ticks.ToString("x") : "0";
            return VirtualPathUtility.ToAbsolute(appRelativePath) + "?v=" + stamp;
        }
    }
}
