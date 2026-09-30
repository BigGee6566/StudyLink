using System;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Account
{
    [RequireSignIn]
    public partial class ChangePassword : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Heading = "Change password";
            Subheading = "Keep your StudyLink account secure.";
            if (!IsPostBack && Query("first") == "1")
                Alert("Please choose your own password before you continue.", AlertKind.Warning);
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            if (Show(S.Auth.ChangePassword(Actor, Current.Text, NewPassword.Text, Confirm.Text)))
                RedirectWith(WebAuth.HomeUrl(CurrentUser.Roles), "Your password has been changed.");
        }
    }
}