using System;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Account
{
    /// <summary>SC5 - sign in with username or email; lands each role on its own dashboard.</summary>
    public partial class Login : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            if (S.Auth.SetupRequired())
            {
                Go("~/Account/Setup.aspx");
                return;
            }
            if (IsSignedIn)
            {
                Go(WebAuth.HomeUrl(CurrentUser.Roles));
                return;
            }
            if (Query("registered") == "student") Success("Your student account is ready. Log in to get started.");
            if (Query("registered") == "tutor") Success("Thanks for registering as a tutor. You can log in now; hub staff will verify your profile before students can book you.");
            if (Query("deactivated") == "1") Fail("Your account is not active. Please contact the hub.");
            if (Query("signedout") == "1") Alert("You have signed out.", AlertKind.Info);
            if (Query("reset") == "1") Success("Your password has been changed. Log in with your new password.");
            if (Query("setup") == "1") Success("The System Admin account is ready. Log in with the details you just chose.");
        }

        protected void Login_Click(object sender, EventArgs e)
        {
            var result = S.Auth.SignIn(Identifier.Text, Password.Text, WebAuth.ClientIp(Context));
            if (!result.Succeeded)
            {
                Fail(result.Error);
                return;
            }

            WebAuth.SignIn(Context, result, Remember.Checked);
            if (result.MustChangePassword)
            {
                Go("~/Account/ChangePassword.aspx?first=1");
                return;
            }
            var returnUrl = Request.QueryString["ReturnUrl"];
            Go(WebAuth.IsLocalUrl(returnUrl) ? returnUrl : WebAuth.HomeUrl(result.Roles));
        }
    }
}