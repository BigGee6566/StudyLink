using System;
using System.Configuration;
using System.Web;
using System.Web.Hosting;
using StudyLink.Data;
using StudyLink.Services;
using StudyLink.Services.Implementations;
using StudyLink.Services.Infrastructure;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>
    /// Per-request composition root: one DbContext and one set of services per HTTP request,
    /// disposed in Application_EndRequest. Pages reach it through BasePage.S.
    /// </summary>
    public static class AppServices
    {
        private const string Key = "StudyLink.Services";
        private static readonly IClock Clock = new SystemClock();

        public static StudyLinkServices Current
        {
            get
            {
                var ctx = HttpContext.Current ?? throw new InvalidOperationException("No HTTP request in progress.");
                if (!(ctx.Items[Key] is StudyLinkServices services))
                {
                    services = Create();
                    ctx.Items[Key] = services;
                }
                return services;
            }
        }

        /// <summary>A fresh, caller-owned set of services (background sweep, demo seeding).</summary>
        public static StudyLinkServices Create()
        {
            var from = ConfigurationManager.AppSettings["StudyLink:MailFrom"] ?? "StudyLink <no-reply@studylink.local>";
            var pickup = string.Equals(ConfigurationManager.AppSettings["StudyLink:MailMode"], "Smtp", StringComparison.OrdinalIgnoreCase)
                ? null
                : HostingEnvironment.MapPath("~/App_Data/MailDrop");
            return new StudyLinkServices(
                new StudyLinkDbContext(),
                Clock,
                new SimulatedCardGateway(Clock),
                new SmtpEmailSender(from, pickup, SmtpFromSettings()),
                new DiskFileStore(HostingEnvironment.MapPath("~/App_Data")));
        }

        /// <summary>
        /// The mail server from App_Data\local.settings.config (git-ignored, so the password never reaches
        /// the repository). Null when Smtp:Host is not set: Web.config's &lt;mailSettings&gt; then apply.
        /// </summary>
        private static SmtpServer SmtpFromSettings()
        {
            var settings = ConfigurationManager.AppSettings;
            if (string.IsNullOrWhiteSpace(settings["Smtp:Host"])) return null;
            return new SmtpServer
            {
                Host = settings["Smtp:Host"].Trim(),
                Port = int.TryParse(settings["Smtp:Port"], out var port) ? port : 587,
                EnableSsl = !string.Equals(settings["Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase),
                UserName = settings["Smtp:UserName"],
                Password = settings["Smtp:Password"]
            };
        }

        public static void EndRequest(HttpContext ctx)
        {
            if (ctx.Items[Key] is StudyLinkServices services)
            {
                ctx.Items.Remove(Key);
                services.Dispose();
            }
        }
    }
}