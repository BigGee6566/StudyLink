using System;
using System.Globalization;
using System.Linq;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Tutor
{
    /// <summary>Reviews students left for this tutor (moderated reviews are hidden, as on the public profile).</summary>
    [RequirePermission(Permission.RespondToBookings)]
    public partial class Reviews : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "What students say about your sessions.";
            var tutorId = S.Profiles.Get(CurrentUser.UserId).TutorId ?? 0;
            var reviews = S.Reviews.ForTutor(tutorId);
            var avg = reviews.Count == 0 ? (decimal?)null : (decimal)reviews.Average(r => r.Rating);
            Summary.Text = Ui.Kpis(this, new[]
            {
                new Kpi("Average rating", avg.HasValue ? avg.Value.ToString("0.0", CultureInfo.InvariantCulture) + " / 5" : "–", null, null, "orange"),
                new Kpi("Reviews", reviews.Count.ToString(CultureInfo.InvariantCulture), null, null, "navy")
            });
            List.DataSource = reviews;
            List.DataBind();
            EmptyState.Text = reviews.Count == 0 ? Ui.Empty("star", "No reviews yet. Students can review a session once it is completed.") : "";
        }
    }
}