using System;
using System.Text;
using System.Web;
using System.Web.UI;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.MasterPages
{
    /// <summary>Public shell for the landing page and the anonymous catalogue.</summary>
    public partial class Public : MasterPage, IAlertHost
    {
        private readonly StringBuilder _alerts = new StringBuilder();

        protected string DashboardUrl { get; private set; } = "~/Default.aspx";

        /// <summary>The landing page draws its own hero.</summary>
        public bool ShowHero { get; set; } = true;

        /// <summary>Pages sit in the 1180px content column; the landing page draws full-width sections instead.</summary>
        public bool ContainContent { get; set; } = true;

        private PublicContact _contact;
        protected PublicContact Contact => _contact ?? (_contact = PublicContact.Load(AppServices.Current.Settings));

        /// <summary>aria-current for the navigation link of the page being viewed.</summary>
        protected string Current(string appRelativeUrl) =>
            string.Equals(Request.AppRelativeCurrentExecutionFilePath, appRelativeUrl, StringComparison.OrdinalIgnoreCase) ? "aria-current=\"page\"" : "";

        public void ShowAlert(string message, AlertKind kind)
        {
            var css = kind == AlertKind.Success ? "success" : kind == AlertKind.Error ? "error" : kind == AlertKind.Warning ? "warning" : "info";
            _alerts.Append("<div class=\"alert-box alert-box--").Append(css).Append("\" role=\"status\"><div>")
                   .Append(HttpUtility.HtmlEncode(message)).Append("</div></div>");
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            var user = WebAuth.Current(Context);
            AnonLinks.Visible = user == null;
            UserLinks.Visible = user != null;
            if (user != null) DashboardUrl = WebAuth.HomeUrl(user.Roles);

            var page = Page as BasePage;
            HeroHolder.Visible = ShowHero;
            HeroTitle.Text = HttpUtility.HtmlEncode(page?.Heading ?? Page.Title);
            HeroSubtitle.Text = HttpUtility.HtmlEncode(page?.Subheading);
            Page.Title = (string.IsNullOrEmpty(Page.Title) ? "" : Page.Title + " · ") + "StudyLink";

            AlertArea.Text = _alerts.ToString();
            AlertHolder.Visible = _alerts.Length > 0;
        }
    }
}