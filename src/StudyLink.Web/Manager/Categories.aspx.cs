using System;
using System.Linq;
using System.Web.UI.WebControls;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Manager
{
    /// <summary>Book categories used by the catalogue filter and reports.</summary>
    [RequirePermission(Permission.ManageBooks)]
    public partial class Categories : BasePage
    {
        private int? EditId => QueryInt("edit");

        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Group textbooks so students can browse them.";
            if (IsPostBack) return;
            var rows = S.Inventory.Categories(Actor);
            Rows.DataSource = rows;
            Rows.DataBind();
            var editing = EditId.HasValue ? rows.FirstOrDefault(r => r.CategoryId == EditId.Value) : null;
            if (editing == null) return;
            FormTitle.Text = "Edit category";
            CategoryName.Text = editing.Name;
            CategoryDescription.Text = editing.Description;
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            if (Show(S.Inventory.SaveCategory(Actor, EditId, CategoryName.Text, CategoryDescription.Text)))
                RedirectWith("~/Manager/Categories.aspx", "Category saved.");
        }

        protected void Row_Command(object source, RepeaterCommandEventArgs e)
        {
            var parts = ((string)e.CommandArgument).Split(':');
            if (e.CommandName != "toggle" || !int.TryParse(parts[0], out var id)) return;
            var active = !bool.Parse(parts[1]);
            if (Show(S.Inventory.SetCategoryActive(Actor, id, active)))
                RedirectWith("~/Manager/Categories.aspx", active ? "Category restored." : "Category retired.");
        }
    }
}
