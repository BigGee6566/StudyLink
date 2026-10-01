using System;
using System.Web.UI.WebControls;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Operations
{
    /// <summary>Review moderation: hidden reviews disappear from profiles and from rating averages.</summary>
    [RequirePermission(Permission.ModerateReviews)]
    public partial class Reviews : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Moderate student reviews of tutors.";
            TabStrip.Text = Ui.Tabs(Request, "view", ("", "All reviews"), ("hidden", "Hidden"));
            if (!IsPostBack) Bind();
        }

        private void Bind()
        {
            var rows = S.Reviews.All(Actor, Query("view") == "hidden");
            Rows.DataSource = rows;
            Rows.DataBind();
            EmptyState.Text = rows.Count == 0 ? Ui.Empty("star", "No reviews here.") : "";
        }

        protected void Row_Command(object source, RepeaterCommandEventArgs e)
        {
            if (!int.TryParse((string)e.CommandArgument, out var id)) return;
            var hide = e.CommandName == "hide";
            Show(S.Reviews.SetHidden(Actor, id, hide, Reason.Text), hide ? "Review hidden." : "Review restored.");
            Bind();
        }
    }
}
