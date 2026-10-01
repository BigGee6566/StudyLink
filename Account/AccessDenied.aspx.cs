using System;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Account
{
    public partial class AccessDenied : BasePage
    {
        protected string HomeUrl => IsSignedIn ? WebAuth.HomeUrl(CurrentUser.Roles) : "~/Default.aspx";

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.StatusCode = 403;
            Response.TrySkipIisCustomErrors = true;
            Heading = "Access denied";
            Subheading = "This page is not available for your role.";
        }
    }
}