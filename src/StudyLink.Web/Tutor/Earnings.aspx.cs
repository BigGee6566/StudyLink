using System;
using System.Globalization;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Tutor
{
    /// <summary>Earnings = completed, paid session fees less the hub's commission (setting TutorCommissionPercent).</summary>
    [RequirePermission(Permission.SubmitTimesheet)]
    public partial class Earnings : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "What you've earned from completed sessions.";
            if (IsPostBack) return;
            var today = DateTime.Today;
            From.Text = Ui.IsoDate(Ui.Date(Query("from")) ?? new DateTime(today.Year, today.Month, 1));
            To.Text = Ui.IsoDate(Ui.Date(Query("to")) ?? today);
            Bind();
        }

        protected void Show_Click(object sender, EventArgs e) =>
            Go("~/Tutor/Earnings.aspx?from=" + From.Text + "&to=" + To.Text);

        private void Bind()
        {
            var from = Ui.Date(From.Text) ?? DateTime.Today.AddDays(-30);
            var to = Ui.Date(To.Text) ?? DateTime.Today;
            var v = S.Timesheets.Earnings(Actor, from, to);
            Kpis.Text = Ui.Kpis(this, new[]
            {
                new Kpi("Sessions completed", v.Sessions.ToString(CultureInfo.InvariantCulture)),
                new Kpi("Hours tutored", Fmt.Hours(v.Hours), "Approved timesheets: " + Fmt.Hours(v.ApprovedTimesheetHours), null, "navy"),
                new Kpi("Session fees", Fmt.Money(v.Gross), null, null, "orange"),
                new Kpi("Your earnings", Fmt.Money(v.Net), "After " + v.CommissionPercent.ToString("0.#", CultureInfo.InvariantCulture) + "% commission", null, "green")
            });
            CommissionNote.Text = "Hub commission " + v.CommissionPercent.ToString("0.#", CultureInfo.InvariantCulture) + "% · " + Fmt.Money(v.Commission);
            Lines.DataSource = v.Lines;
            Lines.DataBind();
            EmptyState.Text = v.Lines.Count == 0 ? Ui.Empty("coins", "No completed sessions in this period.") : "";
        }
    }
}