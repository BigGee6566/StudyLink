using System;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Admin
{
    /// <summary>SC28 - the append-only audit trail: who did what, to which record, when and from where.</summary>
    [RequirePermission(Permission.ViewAuditLogs)]
    public partial class AuditLog : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Every important change, newest first. Entries cannot be edited or deleted.";
            if (IsPostBack) return;
            Ui.FillText(ActionFilter, S.Audit.ActionNames(Actor), "All actions", Fmt.Words);
            Ui.Select(ActionFilter, Query("action"));
            EntityFilter.Text = Query("entity");
            ActorFilter.Text = Query("user");
            From.Text = Query("from");
            To.Text = Query("to");

            var result = S.Audit.Search(Actor, new AuditFilter
            {
                Action = string.IsNullOrEmpty(ActionFilter.SelectedValue) ? null : ActionFilter.SelectedValue,
                EntityType = EntityFilter.Text,
                Actor = ActorFilter.Text,
                From = Ui.Date(From.Text),
                To = Ui.Date(To.Text),
                Page = QueryInt("page") ?? 1,
                PageSize = 30
            });
            Rows.DataSource = result.Items;
            Rows.DataBind();
            EmptyState.Text = result.Total == 0 ? Ui.Empty("audit", "No audit entries match.") : "";
            Pager.Text = Ui.Pager(Request, result);
        }

        protected void Search_Click(object sender, EventArgs e) =>
            Go("~/Admin/AuditLog.aspx?action=" + Server.UrlEncode(ActionFilter.SelectedValue) + "&entity=" + Server.UrlEncode(EntityFilter.Text.Trim()) +
               "&user=" + Server.UrlEncode(ActorFilter.Text.Trim()) + "&from=" + From.Text + "&to=" + To.Text);

        protected static string Change(object item)
        {
            var r = (AuditRow)item;
            var change = string.IsNullOrEmpty(r.PreviousState) && string.IsNullOrEmpty(r.NewState) ? ""
                : Fmt.H(r.PreviousState ?? "–") + " → " + Fmt.H(r.NewState ?? "–");
            return change + (string.IsNullOrEmpty(r.Details) ? "" : "<div class=\"muted\">" + Fmt.H(r.Details) + "</div>");
        }
    }
}
