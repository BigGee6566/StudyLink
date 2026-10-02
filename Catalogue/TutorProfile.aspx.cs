using System;
using System.Linq;
using StudyLink.Data.Entities;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Catalogue
{
    /// <summary>A verified tutor's public profile: subjects, rate, reviews (hidden ones excluded) and open slots.</summary>
    public partial class TutorProfile : CataloguePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var id = QueryInt("id") ?? 0;
            var p = S.TutorSearch.Profile(id);
            if (p == null || p.Status != TutorVerificationStatus.Verified)
            {
                RedirectWith("~/Catalogue/Tutors.aspx", "That tutor is not available for booking.", AlertKind.Warning);
                return;
            }

            Title = p.Card.FullName;
            Heading = p.Card.FullName;
            Subheading = p.Card.Qualification;
            TutorAvatar.Text = Ui.Avatar(p.Card.UserId, p.Card.PhotoVersion, "avatar--lg");
            FullName.Text = H(p.Card.FullName);
            Qualification.Text = H(p.Card.Qualification);
            Rating.Text = Ui.Stars(p.Card.AverageRating, p.Card.ReviewCount);
            Bio.Text = H(string.IsNullOrWhiteSpace(p.Bio) ? "This tutor has not written a bio yet." : p.Bio);
            SubjectChips.Text = string.Concat(p.Subjects.Select(s => "<span class=\"chip\">" + H(s.Subject) + " · " + H(s.StudyLevel) + "</span>"));
            Rate.Text = Fmt.Money(p.Card.HourlyRate) + " per hour";
            CampusName.Text = H(p.Card.Campus);
            Experience.Text = p.Card.ExperienceYears + " years";

            ReviewList.DataSource = p.Reviews.Take(10);
            ReviewList.DataBind();
            NoReviews.Text = p.Reviews.Count == 0 ? "<p class=\"muted mb-0\">No reviews yet.</p>" : "";

            SlotList.DataSource = p.OpenSlots.Take(6);
            SlotList.DataBind();
            NoSlots.Text = p.OpenSlots.Count == 0 ? "<p class=\"muted\">No open slots published right now.</p>" : "";
            BookLink.HRef = ResolveUrl("~/Student/Tutoring/SelectSlot.aspx?tutor=" + id);
            BookLink.Visible = p.OpenSlots.Count > 0 && (!IsSignedIn || CurrentUser.Roles.Contains(Data.Seed.RoleNames.Student));
        }
    }
}