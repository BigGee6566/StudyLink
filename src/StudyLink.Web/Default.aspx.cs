using System;
using System.Linq;
using StudyLink.Data.Entities;
using StudyLink.Web.Infrastructure;
using StudyLink.Web.MasterPages;

namespace StudyLink.Web
{
    /// <summary>SC1 - public landing page: logo, tagline, campus selector, sign-in and registration.</summary>
    public partial class Default : BasePage
    {
        protected int SelectedCampus { get; private set; }

        protected void Page_Init(object sender, EventArgs e)
        {
            if (Master is Public master) master.ShowHero = false;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var campuses = S.Lookups.Campuses();
            SelectedCampus = CampusPreference.Get(Context) ?? campuses.First().Id;
            CampusList.DataSource = campuses;
            CampusList.DataBind();
            BookCount.Text = S.Db.Books.Count(b => b.IsActive).ToString();
            TutorCount.Text = S.Db.Tutors.Count(t => t.VerificationStatus == TutorVerificationStatus.Verified && t.User.IsActive).ToString();
        }

        protected void BrowseBooks_Click(object sender, EventArgs e) => Continue("~/Catalogue/Books.aspx");

        protected void FindTutor_Click(object sender, EventArgs e) => Continue("~/Catalogue/Tutors.aspx");

        private void Continue(string url)
        {
            if (int.TryParse(Request.Form["campus"], out var campusId) && S.Lookups.Campuses().Any(c => c.Id == campusId))
                CampusPreference.Set(Context, campusId);
            Go(url);
        }
    }
}