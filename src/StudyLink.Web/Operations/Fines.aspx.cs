using System;
using System.Linq;
using StudyLink.Data.Entities;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Operations
{
    /// <summary>Overdue, damage and lost-book fines. Waiving cancels the matching unpaid fee and is audited.</summary>
    [RequirePermission(Permission.ManageFines)]
    public partial class Fines : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Fines raised on returns, with waivers.";
            TabStrip.Text = Ui.Tabs(Request, "status", ("", "Outstanding"), ("Paid", "Paid"), ("Waived", "Waived"), ("all", "All"));
            if (IsPostBack) return;
            SearchText.Text = Query("q");
            Bind();
        }

        private void Bind()
        {
            var statusText = Query("status");
            var status = statusText == "all" ? (FineStatus?)null : Ui.EnumOf<FineStatus>(statusText) ?? FineStatus.Outstanding;
            var rows = S.Fines.Search(Actor, status, SearchText.Text);
            Rows.DataSource = rows;
            Rows.DataBind();
            EmptyState.Text = rows.Count == 0 ? Ui.Empty("alert", "No fines in this list.") : "";
            Total.Text = Fmt.Money(rows.Sum(r => r.Amount));

            var waive = QueryInt("waive") is int id ? S.Fines.Search(Actor, FineStatus.Outstanding, null).FirstOrDefault(f => f.FineId == id) : null;
            WaivePanel.Visible = waive != null;
            if (waive != null)
                WaiveSummary.Text = H(Fmt.Words(waive.Type)) + " fine of <strong>" + Fmt.Money(waive.Amount) + "</strong> for " + H(waive.StudentName) + " (" + H(waive.BookTitle) + ").";
        }

        protected void Search_Click(object sender, EventArgs e) => Go(Ui.WithQuery(Request, "q", SearchText.Text.Trim()));

        protected void Waive_Click(object sender, EventArgs e)
        {
            if (Show(S.Fines.Waive(Actor, QueryInt("waive") ?? 0, WaiveReason.Text)))
                RedirectWith("~/Operations/Fines.aspx", "Fine waived. The student has been notified.");
            else Bind();
        }
    }
}
