using System;
using System.Linq;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Admin
{
    /// <summary>SC21 - accounts, roles, recent audit activity, backups and lockouts.</summary>
    [RequirePermission(Permission.ManageAllUsers)]
    public partial class Dashboard : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var d = S.Dashboards.Admin(Actor);
            Heading = "Hi, " + d.FirstName + "!";
            Subheading = "System administration · " + Fmt.Date(DateTime.Today);
            Kpis.Text = Ui.Kpis(this, d.Kpis);
            Audit.DataSource = d.RecentAudit.Take(10);
            Audit.DataBind();
            Roles.DataSource = d.Roles;
            Roles.DataBind();
            DbName.Text = H(d.DatabaseName);
            DbSize.Text = H(d.DatabaseSize);
            LastBackup.Text = d.LastBackup == null ? "<span class=\"badge badge--danger\">Never</span>"
                : Fmt.DateTime(d.LastBackup.StartedAt) + " " + Fmt.Badge(d.LastBackup.Status);
            Locked.Text = d.LockedAccounts.Count == 0 ? "None" : "<a href=\"" + ResolveUrl("~/Admin/Security.aspx") + "\">" + d.LockedAccounts.Count + "</a>";
        }
    }
}
