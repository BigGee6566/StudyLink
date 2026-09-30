using System;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.ErrorPages
{
    public partial class Error404 : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            Heading = "Page not found";
        }
    }
}