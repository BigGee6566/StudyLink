using System;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.ErrorPages
{
    /// <summary>Friendly error page: shows only the log reference, never the exception (brief §24).</summary>
    public partial class Error500 : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.StatusCode = 500;
            Response.TrySkipIisCustomErrors = true;
            Heading = "Something went wrong";
            Reference.Text = H(Context.Items["ErrorReference"] ?? "–");
        }
    }
}