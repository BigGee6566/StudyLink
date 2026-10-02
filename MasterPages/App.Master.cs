using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.MasterPages
{
    /// <summary>Signed-in shell: role navigation, notification bell, greeting band, alerts, mobile tab bar.</summary>
    public partial class App : MasterPage, IAlertHost
    {
        private readonly StringBuilder _alerts = new StringBuilder();

        protected string HomeUrl { get; private set; } = "~/Default.aspx";

        public void ShowAlert(string message, AlertKind kind)
        {
            var css = kind == AlertKind.Success ? "success" : kind == AlertKind.Error ? "error" : kind == AlertKind.Warning ? "warning" : "info";
            _alerts.Append("<div class=\"alert-box alert-box--").Append(css).Append("\" role=\"")
                   .Append(kind == AlertKind.Error ? "alert" : "status").Append("\">")
                   .Append(Icons.Svg(kind == AlertKind.Success ? "check" : kind == AlertKind.Error ? "alert" : "info"))
                   .Append("<div>").Append(HttpUtility.HtmlEncode(message)).Append("</div></div>");
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            var user = WebAuth.Current(Context);
            if (user == null) return;

            HomeUrl = WebAuth.HomeUrl(user.Roles);
            UserAvatar.Text = Ui.Avatar(user.UserId, AppServices.Current.Profiles.PhotoVersion(user.UserId));
            UserName.Text = HttpUtility.HtmlEncode(user.DisplayName);
            UserRole.Text = HttpUtility.HtmlEncode(NavigationMap.RoleLabel(user.Roles));

            var services = AppServices.Current;
            var campus = services.Db.Users.Where(u => u.UserId == user.UserId).Select(u => u.Campus.Name).FirstOrDefault();
            CampusName.Text = campus == null ? "" : Icons.Svg("pin", 14) + " " + HttpUtility.HtmlEncode(campus) + " campus";

            var current = Request.Url.PathAndQuery;
            var items = NavigationMap.For(user.Roles);
            var nav = new StringBuilder();
            var shownSecondary = false;
            foreach (var item in items)
            {
                if (item.Secondary && !shownSecondary)
                {
                    nav.Append("<div class=\"sidebar__section\">More</div>");
                    shownSecondary = true;
                }
                nav.Append(Link(item, current, "sidebar__link", false));
            }
            NavLinks.Text = nav.ToString();
            TabLinks.Text = string.Concat(NavigationMap.Tabs(user.Roles).Select(i => Link(i, current, "", true)));

            var page = Page as BasePage;
            var heading = page?.Heading ?? Page.Title;
            HeroTitle.Text = HttpUtility.HtmlEncode(heading);
            TopTitle.Text = HttpUtility.HtmlEncode(Page.Title);
            HeroSubtitle.Text = HttpUtility.HtmlEncode(page?.Subheading);
            HeroSubtitleHolder.Visible = !string.IsNullOrEmpty(page?.Subheading);
            Page.Title = (string.IsNullOrEmpty(Page.Title) ? "" : Page.Title + " · ") + "StudyLink";

            var unread = services.Notifications.UnreadCount(user.UserId);
            BellCount.Text = unread > 0 ? "<span class=\"bell__count\">" + (unread > 99 ? "99+" : unread.ToString()) + "</span>" : "";
            var latest = services.Notifications.Latest(user.UserId, 6);
            NotifItems.Text = latest.Count == 0
                ? "<div class=\"notif-panel__item muted\">You're all caught up.</div>"
                : string.Concat(latest.Select(n =>
                    "<a class=\"notif-panel__item" + (n.IsRead ? "" : " is-unread") + "\" href=\"" +
                    HttpUtility.HtmlAttributeEncode(ResolveUrl("~/Notifications/Index.aspx?open=" + n.NotificationId)) + "\">" +
                    "<div class=\"notif-panel__title\">" + HttpUtility.HtmlEncode(n.Title) + "</div>" +
                    "<div class=\"notif-panel__msg\">" + HttpUtility.HtmlEncode(n.Message) + " · " + Fmt.Ago(n.CreatedAt) + "</div></a>"));

            AlertArea.Text = _alerts.ToString();
        }

        private string Link(NavItem item, string current, string css, bool tab)
        {
            var url = ResolveUrl(item.Url);
            var isCurrent = IsCurrent(url, current);
            return "<a class=\"" + css + "\" href=\"" + HttpUtility.HtmlAttributeEncode(url) + "\"" + (isCurrent ? " aria-current=\"page\"" : "") + ">" +
                   Icons.Svg(item.Icon, tab ? 22 : 20) + (tab ? "<span>" : " ") + HttpUtility.HtmlEncode(tab && item.Text.Contains("&") ? item.Text.Split(' ')[0] : item.Text) + (tab ? "</span>" : "") + "</a>";
        }

        private static bool IsCurrent(string itemUrl, string current)
        {
            var itemPath = itemUrl.Split('?')[0];
            var currentPath = current.Split('?')[0];
            if (!string.Equals(itemPath, currentPath, StringComparison.OrdinalIgnoreCase)) return false;
            var itemQuery = itemUrl.Contains("?") ? itemUrl.Substring(itemUrl.IndexOf('?')) : "";
            var currentQuery = current.Contains("?") ? current.Substring(current.IndexOf('?')) : "";
            // "?view=..." / "?role=..." items are distinct menu entries on the same page.
            return itemQuery.Length == 0 ? !currentQuery.Contains("view=") && !currentQuery.Contains("role=") : currentQuery.StartsWith(itemQuery, StringComparison.OrdinalIgnoreCase);
        }

        protected void SignOut_Click(object sender, EventArgs e)
        {
            WebAuth.SignOut(Context);
            Response.Redirect("~/Account/Login.aspx?signedout=1", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}