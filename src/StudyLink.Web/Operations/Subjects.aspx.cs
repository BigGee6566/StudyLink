using System;
using System.Linq;
using System.Web.UI.WebControls;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Operations
{
    /// <summary>The tutoring subject catalogue (Manager / System Admin).</summary>
    [RequirePermission(Permission.ManageSubjects)]
    public partial class Subjects : BasePage
    {
        private int? EditId => QueryInt("edit");

        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Subjects students and tutors can choose.";
            if (IsPostBack) return;
            var rows = S.Subjects.Catalogue(Actor);
            Rows.DataSource = rows;
            Rows.DataBind();
            var editing = EditId.HasValue ? rows.FirstOrDefault(r => r.SubjectId == EditId.Value) : null;
            if (editing != null)
            {
                FormTitle.Text = "Rename subject";
                SubjectName.Text = editing.Name;
            }
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            var result = EditId.HasValue ? S.Subjects.RenameSubject(Actor, EditId.Value, SubjectName.Text) : S.Subjects.AddSubject(Actor, SubjectName.Text);
            if (Show(result)) RedirectWith("~/Operations/Subjects.aspx", EditId.HasValue ? "Subject renamed." : "Subject added.");
        }

        protected void Row_Command(object source, RepeaterCommandEventArgs e)
        {
            var parts = ((string)e.CommandArgument).Split(':');
            if (e.CommandName != "toggle" || !int.TryParse(parts[0], out var id)) return;
            var active = !bool.Parse(parts[1]);
            if (Show(S.Subjects.SetSubjectActive(Actor, id, active)))
                RedirectWith("~/Operations/Subjects.aspx", active ? "Subject restored." : "Subject retired.");
        }
    }
}
