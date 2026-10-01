using System;
using System.Linq;
using StudyLink.Data.Entities;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Operations
{
    /// <summary>SC27 - staff verify the person, then the system emails a single-use reset link (no temporary passwords).</summary>
    [RequirePermission(Permission.HandlePasswordResets)]
    public partial class PasswordResets : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Verify the person before a reset link is sent.";
            TabStrip.Text = Ui.Tabs(Request, "status", ("", "Pending"), ("Approved", "Approved"), ("Rejected", "Rejected"), ("all", "All"));
            if (!IsPostBack) Bind();
        }

        private void Bind()
        {
            var statusText = Query("status");
            var status = statusText == "all" ? (RequestStatus?)null : Ui.EnumOf<RequestStatus>(statusText) ?? RequestStatus.Pending;
            var rows = S.PasswordResets.List(Actor, status);
            Rows.DataSource = rows;
            Rows.DataBind();
            EmptyState.Text = rows.Count == 0 ? Ui.Empty("key", "No requests in this list.") : "";

            var id = QueryInt("id");
            var r = id.HasValue ? S.PasswordResets.List(Actor, RequestStatus.Pending).FirstOrDefault(x => x.RequestId == id.Value) : null;
            HandlePanel.Visible = r != null;
            if (r == null) return;
            Identifier.Text = H(r.Identifier);
            Matched.Text = r.UserId.HasValue ? H(r.MatchedUser + " · " + r.MatchedEmail + " · " + r.Roles) : "<span class=\"badge badge--danger\">No account matches</span>";
            Message.Text = H(r.Message ?? "–");
            Requested.Text = Fmt.DateTime(r.CreatedAt) + " from " + H(r.RequestIp);
            ApproveButton.Visible = r.UserId.HasValue;
        }

        protected void Approve_Click(object sender, EventArgs e)
        {
            if (Show(S.PasswordResets.Approve(Actor, QueryInt("id") ?? 0, Note.Text, ResetLinkBase)))
                RedirectWith("~/Operations/PasswordResets.aspx", "Approved. A single-use reset link has been emailed to the account holder.");
            else Bind();
        }

        protected void Reject_Click(object sender, EventArgs e)
        {
            if (Show(S.PasswordResets.Reject(Actor, QueryInt("id") ?? 0, Note.Text)))
                RedirectWith("~/Operations/PasswordResets.aspx", "Request rejected.");
            else Bind();
        }
    }
}
