using System;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Account
{
    /// <summary>UC3 Reset Password: files a request that Staff verify (SC27). Never reveals whether an account exists.</summary>
    public partial class ForgotPassword : BasePage
    {
        protected void Send_Click(object sender, EventArgs e)
        {
            var result = S.PasswordResets.Request(Identifier.Text, Message.Text, WebAuth.ClientIp(Context));
            if (!Show(result)) return;
            FormPanel.Visible = false;
            Success("Thank you. If that account exists, hub staff will verify your request and email you a reset link. You can also visit the hub desk with your student card.");
        }
    }
}