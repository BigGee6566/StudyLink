using System;
using StudyLink.Data.Seed;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Admin
{
    /// <summary>On-demand SQL Server BACKUP DATABASE, with every run logged in BackupLogs.</summary>
    [RequirePermission(Permission.ManageBackups)]
    public partial class Backups : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Back up the StudyLink database and review past backups.";
            if (!IsPostBack) Bind();
        }

        private void Bind()
        {
            var folder = S.Settings.GetString(SettingKeys.BackupDirectory, "");
            Target.Text = string.IsNullOrWhiteSpace(folder) ? "Saved to SQL Server's default backup folder." : "Saved to " + H(folder) + " on the database server.";
            var rows = S.Backups.History(Actor);
            Rows.DataSource = rows;
            Rows.DataBind();
            EmptyState.Text = rows.Count == 0 ? Ui.Empty("database", "No backups have been taken yet.") : "";
        }

        protected void Run_Click(object sender, EventArgs e)
        {
            Show(S.Backups.Run(Actor), "Backup completed.");
            Bind();
        }
    }
}
