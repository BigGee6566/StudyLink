using System;
using System.Text;
using System.Web;
using System.Web.UI;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.MasterPages
{
    /// <summary>Split-panel shell for sign-in, registration and password pages (SC2-SC5).</summary>
    public partial class Auth : MasterPage, IAlertHost
    {
        private readonly StringBuilder _alerts = new StringBuilder();

        public void ShowAlert(string message, AlertKind kind)
        {
            var css = kind == AlertKind.Success ? "success" : kind == AlertKind.Error ? "error" : kind == AlertKind.Warning ? "warning" : "info";
            _alerts.Append("<div class=\"alert-box alert-box--").Append(css).Append("\" role=\"")
                   .Append(kind == AlertKind.Error ? "alert" : "status").Append("\"><div>")
                   .Append(HttpUtility.HtmlEncode(message)).Append("</div></div>");
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            AlertArea.Text = _alerts.ToString();
            Page.Title = (string.IsNullOrEmpty(Page.Title) ? "" : Page.Title + " · ") + "StudyLink";
        }
    }
}