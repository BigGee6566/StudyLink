using System;
using System.Configuration;
using System.Data.Entity;
using System.Web;
using StudyLink.Data;
using StudyLink.Services.Infrastructure;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // The site brings its own database up to date (same migrations as database\01_CreateSchema.sql).
            if (!string.Equals(ConfigurationManager.AppSettings["StudyLink:MigrateOnStart"], "false", StringComparison.OrdinalIgnoreCase))
                Database.SetInitializer(new MigrateDatabaseToLatestVersion<StudyLinkDbContext, StudyLink.Data.Migrations.Configuration>(true));
            else
                Database.SetInitializer<StudyLinkDbContext>(null);

            try
            {
                using (var db = new StudyLinkDbContext())
                    db.Database.Initialize(force: false);
                DemoSeeder.RunIfRequested();
            }
            catch (Exception ex)
            {
                // Keep the site up so the error page can explain; the log has the detail.
                ErrorLogger.Log(ex, "Application_Start");
            }

            BackgroundSweep.Start();
        }

        /// <summary>Rebuilds the principal (id, roles, name) from the forms ticket on every request.</summary>
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            WebAuth.AttachPrincipal(Context);
        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            AppServices.EndRequest(Context);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var ex = Server.GetLastError();
            var root = ex?.GetBaseException();

            if (root is AccessDeniedException)
            {
                Server.ClearError();
                Response.Redirect("~/Account/AccessDenied.aspx", endResponse: false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            if (ex is HttpException http && http.GetHttpCode() == 404)
                return; // customErrors shows the friendly 404 page

            Context.Items["ErrorReference"] = ErrorLogger.Log(ex, Request.RawUrl);
        }

        protected void Application_End(object sender, EventArgs e)
        {
            BackgroundSweep.Stop();
        }
    }
}