using System;
using System.IO;
using System.Web.Hosting;
using StudyLink.Services.Implementations;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>
    /// Writes unhandled errors to App_Data\Logs (ported from TutorConnect ErrorLogger). Users only
    /// ever see the short reference; stack traces and connection details stay in the log.
    /// </summary>
    public static class ErrorLogger
    {
        private static readonly object Gate = new object();

        public static string Log(Exception ex, string where)
        {
            var reference = ReferenceGenerator.RandomCode(8);
            try
            {
                var folder = HostingEnvironment.MapPath("~/App_Data/Logs") ?? Path.Combine(Path.GetTempPath(), "StudyLinkLogs");
                Directory.CreateDirectory(folder);
                var file = Path.Combine(folder, "errors-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                var entry = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] ref {1} at {2}{3}{4}{3}{3}", DateTime.Now, reference, where, Environment.NewLine, ex);
                lock (Gate)
                    File.AppendAllText(file, entry);
            }
            catch
            {
                // Logging must never become the error.
            }
            return reference;
        }
    }
}