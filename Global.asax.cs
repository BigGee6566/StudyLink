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
            // The site brings its own database up to date (same migrations as database\StudyLink.sql).
            if (!string.Equals(ConfigurationManager.AppSettings["StudyLink:MigrateOnStart"], "false", StringComparison.OrdinalIgnoreCase))
                Database.SetInitializer(new MigrateDatabaseToLatestVersion<StudyLinkDbContext, StudyLink.Data.Migrations.Configuration>(true));
            else
                Database.SetInitializer<StudyLinkDbContext>(null);

            // Each start-up step is isolated: a failure is logged and the site still starts, so the error
            // page (or the rest of the site) can still explain what is wrong.
            Step("Application_Start: migrations", () =>
            {
                using (var db = new StudyLinkDbContext())
                {
                    db.Database.Initialize(force: false);
                    DisableAutoClose(db);
                }
            });
            Step("Application_Start: demo seed", DemoSeeder.RunIfRequested);
            Step("Application_Start: demo data correction", DemoSeeder.CorrectIfRequested);
            Step("Application_Start: shipped covers", () =>
            {
                using (var db = new StudyLinkDbContext())
                    CoverLibrary.FillMissing(db);
            });

            BackgroundSweep.Start();
        }

        private static void Step(string name, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex, name);
            }
        }

        /// <summary>
        /// SQL Express creates databases with AUTO_CLOSE ON, so the database shuts down whenever the last
        /// connection closes and every reopen delays the next login (timeouts on a busy machine).
        /// ALTER DATABASE cannot run inside a transaction, hence outside the migrations. Needs ALTER
        /// permission; where that is missing the DBA script (database\StudyLink.sql) does it.
        /// </summary>
        private static void DisableAutoClose(StudyLinkDbContext db)
        {
            try
            {
                db.Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction,
                    "IF DATABASEPROPERTYEX(DB_NAME(), 'IsAutoClose') = 1 EXEC (N'ALTER DATABASE CURRENT SET AUTO_CLOSE OFF');");
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex, "DisableAutoClose");
            }
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