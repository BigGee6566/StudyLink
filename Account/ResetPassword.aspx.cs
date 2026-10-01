using System;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Account
{
    /// <summary>Consumes a single-use reset (or account set-up) link emailed after staff approval.</summary>
    public partial class ResetPassword : BasePage
    {
        private string Token => Query("token");

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            var valid = S.PasswordResets.IsTokenValid(Token);
            FormPanel.Visible = valid;
            InvalidPanel.Visible = !valid;
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            var result = S.PasswordResets.ResetWithToken(Token, NewPassword.Text, Confirm.Text, WebAuth.ClientIp(Context));
            if (Show(result))
                Go("~/Account/Login.aspx?reset=1");
        }
    }
}