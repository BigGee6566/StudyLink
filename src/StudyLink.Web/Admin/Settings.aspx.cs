using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Admin
{
    /// <summary>Business rules the hub can tune: rental period, fees, holds, cut-offs, lockout policy, backups.</summary>
    [RequirePermission(Permission.ManageSettings)]
    public partial class Settings : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Rules StudyLink applies across all campuses.";
            if (!IsPostBack) Bind();
        }

        private void Bind()
        {
            Rows.DataSource = S.Settings.List();
            Rows.DataBind();
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            var changed = 0;
            var errors = new List<string>();
            foreach (RepeaterItem item in Rows.Items)
            {
                var key = ((HiddenField)item.FindControl("Key")).Value;
                var original = ((HiddenField)item.FindControl("Original")).Value;
                var value = ((TextBox)item.FindControl("Value")).Text.Trim();
                if (value == original) continue;
                var result = S.Settings.Update(Actor, key, value);
                if (result.Succeeded) changed++;
                else errors.Add(Fmt.Words(key) + ": " + result.Error);
            }
            if (errors.Count > 0)
            {
                Fail(string.Join(" ", errors));
                if (changed > 0) Success(changed + " other setting(s) were saved.");
                Bind();
                return;
            }
            RedirectWith("~/Admin/Settings.aspx", changed == 0 ? "Nothing changed." : changed + " setting(s) saved.", changed == 0 ? AlertKind.Info : AlertKind.Success);
        }
    }
}
