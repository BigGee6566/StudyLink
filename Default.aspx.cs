using System;
using System.Collections.Generic;
using System.Linq;
using StudyLink.Data.Entities;
using StudyLink.Data.Seed;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web
{
    /// <summary>SC1 - public landing page: what StudyLink is, who it is for, the campuses, sign-in and registration.</summary>
    public partial class Default : BasePage
    {
        public sealed class CampusCard
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Image { get; set; }
        }

        /// <summary>Campus name → photo in Images/site (tools/optimize_images.py).</summary>
        private static readonly Dictionary<string, string> CampusImages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Alice"] = "campus-alice",
            ["East London"] = "campus-east-london",
            ["Bisho"] = "campus-bisho"
        };

        protected int SelectedCampus { get; private set; }
        protected int RentalDays { get; private set; }
        protected int MaxRenewals { get; private set; }
        protected IList<CampusCard> CampusCards { get; private set; }

        protected void Page_Init(object sender, EventArgs e)
        {
            if (Master is MasterPages.Public master)
            {
                master.ShowHero = false;
                master.ContainContent = false;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var campuses = S.Lookups.Campuses();
            SelectedCampus = CampusPreference.Get(Context) ?? campuses.First().Id;
            CampusList.DataSource = campuses;
            CampusList.DataBind();
            CampusCards = campuses.Where(c => CampusImages.ContainsKey(c.Name))
                .Select(c => new CampusCard { Id = c.Id, Name = c.Name, Image = CampusImages[c.Name] }).ToList();

            RentalDays = S.Settings.GetInt(SettingKeys.RentalPeriodDays, 7);
            MaxRenewals = S.Settings.GetInt(SettingKeys.MaxRenewals, 2);
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
