using System;
using StudyLink.Services.DTOs;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Account
{
    /// <summary>First-run creation of the first System Admin (ported from TutorConnect Setup.aspx).</summary>
    public partial class Setup : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var open = S.Auth.SetupRequired();
            FormPanel.Visible = open;
            ClosedPanel.Visible = !open;
            if (!IsPostBack && open)
            {
                Campus.DataSource = S.Lookups.Campuses();
                Campus.DataBind();
            }
        }

        protected void Create_Click(object sender, EventArgs e)
        {
            var details = new PersonalDetails
            {
                UserName = UserName.Text, FirstName = FirstName.Text, LastName = LastName.Text, Email = Email.Text,
                CellNumber = Cell.Text, CampusId = int.TryParse(Campus.SelectedValue, out var c) ? c : 0
            };
            if (Show(S.Auth.CreateFirstAdmin(details, Password.Text, Confirm.Text, WebAuth.ClientIp(Context))))
                Go("~/Account/Login.aspx?setup=1");
        }
    }
}