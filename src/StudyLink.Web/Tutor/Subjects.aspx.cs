using System;
using System.Web.UI.WebControls;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Tutor
{
    /// <summary>The subject and level pairs a tutor offers (BR-05: no duplicates; in-use pairs stay).</summary>
    [RequirePermission(Permission.ManageAvailability)]
    public partial class Subjects : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Students search for tutors by these subjects and levels.";
            if (IsPostBack) return;
            Ui.Fill(Subject, S.Lookups.Subjects());
            Ui.Fill(Level, S.Lookups.StudyLevels());
            Bind();
        }

        private void Bind()
        {
            var rows = S.Subjects.TutorSubjects(Actor);
            List.DataSource = rows;
            List.DataBind();
            EmptyState.Text = rows.Count == 0 ? Ui.Empty("cap", "Add at least one subject so students can find you.") : "";
        }

        protected void Add_Click(object sender, EventArgs e)
        {
            Show(S.Subjects.AddTutorSubject(Actor, Ui.Int(Subject) ?? 0, Ui.Int(Level) ?? 0), "Subject added.");
            Bind();
        }

        protected void List_Command(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "remove" && int.TryParse((string)e.CommandArgument, out var id))
                Show(S.Subjects.RemoveTutorSubject(Actor, id), "Subject removed.");
            Bind();
        }
    }
}