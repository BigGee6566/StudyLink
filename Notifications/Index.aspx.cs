using System;
using System.Linq;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Notifications
{
    /// <summary>The bell's full list. ?open=id marks one read and follows its link.</summary>
    [RequireSignIn]
    public partial class Index : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var userId = CurrentUser.UserId;
            if (!IsPostBack && QueryInt("open") is int open)
            {
                var target = S.Db.Notifications.Where(n => n.NotificationId == open && n.UserId == userId).Select(n => n.LinkUrl).FirstOrDefault();
                S.Notifications.MarkRead(userId, open);
                // Only follow app-relative links stored by the services; anything else stays on this page.
                if (!string.IsNullOrEmpty(target) && target.StartsWith("~/", StringComparison.Ordinal))
                {
                    Go(target);
                    return;
                }
            }
            Subheading = "Updates about your sessions, books, payments and account.";
            Bind();
        }

        private void Bind()
        {
            var page = S.Notifications.ForUser(CurrentUser.UserId, QueryInt("page") ?? 1, 20);
            List.DataSource = page.Items;
            List.DataBind();
            EmptyState.Text = page.Total == 0 ? Ui.Empty(EmptyArt.Bell, "You're all caught up", "Booking, payment and due-date updates will appear here.") : "";
            Pager.Text = Ui.Pager(Request, page);
            MarkAll.Visible = page.Items.Any(n => !n.IsRead);
        }

        protected void MarkAll_Click(object sender, EventArgs e)
        {
            S.Notifications.MarkAllRead(CurrentUser.UserId);
            Bind();
        }
    }
}