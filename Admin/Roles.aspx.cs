using System;
using System.Collections.Generic;
using System.Linq;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Admin
{
    /// <summary>The six roles, their members and the permissions each one grants.</summary>
    [RequirePermission(Permission.AssignRoles)]
    public partial class Roles : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "What each role can do, and who holds it.";
            List.DataSource = S.Users.Roles(Actor);
            List.DataBind();
        }

        protected static string Chips(object permissions) =>
            string.Concat(((IEnumerable<string>)permissions).Select(p => "<span class=\"chip\">" + Fmt.H(Fmt.Words(p)) + "</span>"));
    }
}
