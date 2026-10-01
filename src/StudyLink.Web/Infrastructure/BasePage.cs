using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using StudyLink.Services;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Security;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>The page requires this permission (checked against RolePermissions on every request).</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequirePermissionAttribute : Attribute
    {
        public RequirePermissionAttribute(params Permission[] anyOf) { AnyOf = anyOf; }
        public Permission[] AnyOf { get; }
    }

    /// <summary>The page requires a signed-in user of any role.</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RequireSignInAttribute : Attribute { }

    public enum AlertKind { Success, Error, Info, Warning }

    /// <summary>Implemented by the master pages that can display a page-level alert.</summary>
    public interface IAlertHost
    {
        void ShowAlert(string message, AlertKind kind);
    }

    /// <summary>
    /// Base for every StudyLink page. Second of the three access-control layers (web.config folder
    /// rules, this class, then the services' own Demand checks), plus CSRF binding and flash messages.
    /// </summary>
    public abstract class BasePage : Page
    {
        private const string FlashKey = "StudyLink.Flash";
        private const string SessionPinKey = "StudyLink.Session";

        protected StudyLinkServices S => AppServices.Current;
        protected StudyLinkPrincipal CurrentUser => WebAuth.Current(Context);
        protected Actor Actor => WebAuth.ActorFor(Context);
        protected bool IsSignedIn => CurrentUser != null;

        /// <summary>Shown in the navy page header band (the mockups' coloured top section).</summary>
        public string Heading { get; set; }
        public string Subheading { get; set; }

        protected override void OnInit(EventArgs e)
        {
            // CSRF: bind ViewState (and so every postback) to this user's session. A forged
            // cross-site post carries someone else's ViewState and fails MAC validation.
            if (IsSignedIn && Session != null)
            {
                // ASP.NET issues a new SessionID on every request until something is stored in the
                // session, which would make every postback fail MAC validation. Pin the session first.
                if (Session[SessionPinKey] == null) Session[SessionPinKey] = true;
                ViewStateUserKey = CurrentUser.UserId + ":" + Session.SessionID;
            }

            var perms = GetType().GetCustomAttributes(typeof(RequirePermissionAttribute), true).Cast<RequirePermissionAttribute>().ToList();
            var needsSignIn = perms.Any() || GetType().IsDefined(typeof(RequireSignInAttribute), true);
            if (needsSignIn && !IsSignedIn)
            {
                FormsAuthentication.RedirectToLoginPage();
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            if (perms.Any(p => !p.AnyOf.Any(x => RolePermissions.Has(CurrentUser.Roles, x))))
                throw new AccessDeniedException("You do not have access to this page.");

            base.OnInit(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!IsPostBack && Session?[FlashKey] is Tuple<string, AlertKind> flash)
            {
                Session.Remove(FlashKey);
                Alert(flash.Item1, flash.Item2);
            }
            base.OnLoad(e);
        }

        protected void Alert(string message, AlertKind kind = AlertKind.Info)
        {
            if (Master is IAlertHost host) host.ShowAlert(message, kind);
            else if (Master?.Master is IAlertHost outer) outer.ShowAlert(message, kind);
        }

        protected void Success(string message) => Alert(message, AlertKind.Success);
        protected void Fail(string message) => Alert(message, AlertKind.Error);

        /// <summary>Shows a service outcome; returns true when it succeeded.</summary>
        protected bool Show(ServiceResult result, string successMessage = null)
        {
            if (result.Succeeded)
            {
                if (successMessage != null) Success(successMessage);
                return true;
            }
            Fail(result.Error);
            return false;
        }

        /// <summary>Post/Redirect/Get with a message that survives the redirect.</summary>
        protected void RedirectWith(string url, string message, AlertKind kind = AlertKind.Success)
        {
            if (Session != null) Session[FlashKey] = Tuple.Create(message, kind);
            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void Go(string url)
        {
            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected int? QueryInt(string name) => int.TryParse(Request.QueryString[name], out var v) ? v : (int?)null;
        protected string Query(string name) => Request.QueryString[name];

        protected static string H(object value) => HttpUtility.HtmlEncode(Convert.ToString(value));

        /// <summary>Show a service-composed link (e.g. "~/Tutor/Schedule.aspx") as a browser URL.</summary>
        protected string Url(string appRelative) => string.IsNullOrEmpty(appRelative) ? "#" : ResolveUrl(appRelative);

        /// <summary>Absolute reset/setup link prefix the services append the single-use token to.</summary>
        protected string ResetLinkBase => new Uri(Request.Url, ResolveUrl("~/Account/ResetPassword.aspx?token=")).AbsoluteUri;
    }

    /// <summary>
    /// Pages visitors can browse anonymously (books, tutors, help). Signed-in users keep their
    /// navigation shell; visitors get the public header. Both masters expose the same placeholders.
    /// </summary>
    public abstract class CataloguePage : BasePage
    {
        protected override void OnPreInit(EventArgs e)
        {
            MasterPageFile = IsSignedIn ? "~/MasterPages/App.Master" : "~/MasterPages/Public.Master";
            base.OnPreInit(e);
        }

        /// <summary>The campus the visitor chose on the landing page, else their own campus.</summary>
        protected int? PreferredCampus()
        {
            var chosen = CampusPreference.Get(Context);
            if (chosen.HasValue) return chosen;
            if (!IsSignedIn) return null;
            var userId = CurrentUser.UserId;
            return S.Db.Users.Where(u => u.UserId == userId).Select(u => (int?)u.CampusId).FirstOrDefault();
        }
    }
}