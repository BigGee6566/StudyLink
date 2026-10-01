using System;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Public
{
    /// <summary>Contact / Support form. Messages land in the staff support inbox (and are rate limited).</summary>
    public partial class Contact : CataloguePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Questions about books, sessions or your account.";
            if (IsPostBack) return;
            Ui.Fill(Campus, S.Lookups.Campuses(), "Any campus", PreferredCampus());
            if (IsSignedIn)
            {
                var me = S.Profiles.Get(CurrentUser.UserId);
                SenderName.Text = me.FullName;
                SenderEmail.Text = me.Email;
            }
        }

        protected void Send_Click(object sender, EventArgs e)
        {
            var result = S.Support.Submit(IsSignedIn ? Actor : null, SenderName.Text, SenderEmail.Text, Ui.Int(Campus), Subject.Text, Body.Text, WebAuth.ClientIp(Context));
            if (!Show(result)) return;
            FormPanel.Visible = false;
            Sent.Text = Ui.Empty("check", "Thanks, your message has been sent. Hub staff will reply by email.");
        }
    }
}