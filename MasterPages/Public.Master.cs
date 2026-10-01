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