using System;
using System.Linq;
using System.Web.UI.WebControls;
using StudyLink.Data.Entities;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Operations
{
    /// <summary>Support inbox (Contact form messages) and profile-change requests that need staff approval.</summary>
    [RequirePermission(Permission.HandleSupport)]
    public partial class Support : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Messages from students, tutors and visitors.";
            TabStrip.Text = Ui.Tabs(Request, "view", ("", "Open messages"), ("all", "All messages"), ("profile", "Profile change requests"));
            if (!IsPostBack) Bind();
        }

        private void Bind()
        {
            var view = Query("view") ?? "";
            if (view == "profile")
            {
                Views.SetActiveView(ProfileView);
                var requests = S.Profiles.Pending(Actor);
                ProfileRequests.DataSource = requests;
                ProfileRequests.DataBind();
                NoProfileRequests.Text = requests.Count == 0 ? Ui.Empty("user", "No profile change requests.") : "";
                return;
            }

            Views.SetActiveView(MessagesView);
            var messages = S.Support.List(Actor, null).Where(x => view == "all" || x.Status != SupportStatus.Resolved).ToList();
            Messages.DataSource = messages;
            Messages.DataBind();
            NoMessages.Text = messages.Count == 0 ? Ui.Empty("support", "No messages here. All caught up.") : "";

            var id = QueryInt("id");
            var m = id.HasValue ? S.Support.List(Actor, null).FirstOrDefault(x => x.MessageId == id.Value) : null;
            RespondPanel.Visible = m != null;
            if (m == null) return;
            MsgSubject.Text = H(m.Subject);
            MsgStatus.Text = Fmt.Badge(m.Status);
            MsgFrom.Text = H(m.Name + " <" + m.Email + ">") + " · " + Fmt.DateTime(m.CreatedAt) + (m.FromSignedInUser ? " · signed-in user" : "");
            MsgBody.Text = H(m.Body);
            if (NewStatus.Items.Count == 0) Ui.FillEnum<SupportStatus>(NewStatus);
            Ui.Select(NewStatus, ((int)(m.Status == SupportStatus.Open ? SupportStatus.InProgress : m.Status)).ToString());
            ResponseText.Text = m.ResponseNote;
        }

        protected void Respond_Click(object sender, EventArgs e)
        {
            var status = Ui.EnumOf<SupportStatus>(NewStatus) ?? SupportStatus.InProgress;
            if (Show(S.Support.Respond(Actor, QueryInt("id") ?? 0, status, ResponseText.Text)))
                RedirectWith("~/Operations/Support.aspx", "Message updated.");
            else Bind();
        }

        protected void Profile_Command(object source, RepeaterCommandEventArgs e)
        {
            if (!int.TryParse((string)e.CommandArgument, out var id)) return;
            var approve = e.CommandName == "approve";
            Show(S.Profiles.Review(Actor, id, approve, null), approve ? "Change approved and applied." : "Change request rejected.");
            Bind();
        }
    }
}
