using System;
using System.Linq;
using StudyLink.Services.DTOs;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Catalogue
{
    /// <summary>SC7 - search verified, active tutors (pending or suspended tutors never appear).</summary>
    public partial class Tutors : CataloguePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Verified tutors by subject, level and campus.";
            if (IsPostBack) return;
            Ui.Fill(Subject, S.Lookups.Subjects(), "All subjects", QueryInt("subject"));
            Ui.Fill(Level, S.Lookups.StudyLevels(), "All levels", QueryInt("level"));
            Ui.Fill(Campus, S.Lookups.Campuses(), "All campuses", QueryInt("campus") ?? PreferredCampus());
            SearchText.Text = Query("q");

            var result = S.TutorSearch.Search(new TutorSearchFilter
            {
                SubjectId = Ui.Int(Subject),
                StudyLevelId = Ui.Int(Level),
                CampusId = Ui.Int(Campus),
                Text = SearchText.Text,
                Page = QueryInt("page") ?? 1
            });
            TutorList.DataSource = result.Items;
            TutorList.DataBind();
            ResultSummary.Text = result.Total == 1 ? "1 tutor found" : result.Total + " tutors found";
            EmptyState.Text = result.Total == 0 ? Ui.Empty(EmptyArt.Search, "No tutors match these filters yet", "Try another subject, level or campus.") : "";
            Pager.Text = Ui.Pager(Request, result);
        }

        protected void Search_Click(object sender, EventArgs e)
        {
            if (Ui.Int(Campus) is int campus) CampusPreference.Set(Context, campus);
            Go("~/Catalogue/Tutors.aspx?subject=" + Subject.SelectedValue + "&level=" + Level.SelectedValue +
               "&campus=" + Campus.SelectedValue + "&q=" + Server.UrlEncode(SearchText.Text.Trim()));
        }

        protected string BookUrl(int tutorId) =>
            ResolveUrl("~/Student/Tutoring/SelectSlot.aspx?tutor=" + tutorId + (Ui.Int(Subject) is int s ? "&subject=" + s : ""));

        protected static string Chips(string csv) =>
            string.Concat((csv ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => "<span class=\"chip chip--subject\">" + Icons.Subject(s.Trim(), 13) + Fmt.H(s.Trim()) + "</span>"));

        protected static string NextFree(DateTime? next) =>
            next.HasValue ? "Next open slot: " + Fmt.H(Fmt.DateTime(next.Value)) : "No open slots published right now";
    }
}