using System;
using System.Linq;
using System.Web.UI.WebControls;
using StudyLink.Data.Seed;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Admin
{
    /// <summary>Lockout policy, locked accounts (unlock) and recent security-relevant audit events.</summary>
    [RequirePermission(Permission.ManageSecurity)]
    public partial class Security : BasePage
    {
        private static readonly string[] SecurityActions = { "AccountLocked", "AccountUnlocked", "PasswordChanged", "PasswordResetApproved", "RoleAssigned", "RoleRemoved" };

        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Sign-in protection and account lockouts.";
            if (!IsPostBack) Bind();
        }

        private void Bind()
        {
            var attempts = S.Settings.GetInt(SettingKeys.LockoutThreshold, 5);
            var minutes = S.Settings.GetInt(SettingKeys.LockoutMinutes, 15);
            Policy.Text = "<div class=\"alert-box alert-box--info\">" + Icons.Svg("lock") + "<div>Accounts lock for <strong>" + minutes +
                          " minutes</strong> after <strong>" + attempts + "</strong> failed sign-ins in a row. Change this under <a href=\"" +
                          ResolveUrl("~/Admin/Settings.aspx") + "\">System Settings</a>.</div></div>";

            var locked = S.Users.LockedAccounts(Actor);
            LockedList.DataSource = locked;
            LockedList.DataBind();
            NoLocked.Text = locked.Count == 0 ? "<p class=\"muted mb-0\">No accounts are locked.</p>" : "";

            var events = SecurityActions
                .SelectMany(a => S.Audit.Search(Actor, new AuditFilter { Action = a, From = DateTime.Today.AddDays(-30), PageSize = 10 }).Items)
                .OrderByDescending(r => r.CreatedAt).Take(12).ToList();
            EventList.DataSource = events;
            EventList.DataBind();
            NoEvents.Text = events.Count == 0 ? "<p class=\"muted mb-0\">No security events in the last 30 days.</p>" : "";
        }

        protected void Locked_Command(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "unlock" && int.TryParse((string)e.CommandArgument, out var id))
                Show(S.Users.Unlock(Actor, id), "Account unlocked.");
            Bind();
        }
    }
}
