using System;
using StudyLink.Services.DTOs;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Catalogue
{
    /// <summary>SC11 - textbook catalogue with stock counted live from copy statuses at the chosen campus.</summary>
    public partial class Books : CataloguePage
    {
        protected string CampusQuery { get; private set; } = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Rent or buy prescribed textbooks at your campus.";
            if (IsPostBack) return;

            Ui.Fill(Category, S.Lookups.Categories(), "All categories", QueryInt("category"));
            Ui.Fill(Campus, S.Lookups.Campuses(), "All campuses", QueryInt("campus") ?? PreferredCampus());
            SearchText.Text = Query("q");
            AvailableOnly.Checked = Query("available") == "1";
            Bind();
        }

        protected void Search_Click(object sender, EventArgs e)
        {
            if (Ui.Int(Campus) is int campus) CampusPreference.Set(Context, campus);
            Go("~/Catalogue/Books.aspx?q=" + Server.UrlEncode(SearchText.Text.Trim()) + "&category=" + Category.SelectedValue +
               "&campus=" + Campus.SelectedValue + (AvailableOnly.Checked ? "&available=1" : ""));
        }

        private void Bind()
        {
            var campus = Ui.Int(Campus);
            CampusQuery = campus.HasValue ? "&campus=" + campus : "";
            var result = S.Catalogue.Search(new BookFilter
            {
                Text = SearchText.Text,
                CategoryId = Ui.Int(Category),
                CampusId = campus,
                AvailableOnly = AvailableOnly.Checked,
                Page = QueryInt("page") ?? 1
            });
            BookList.DataSource = result.Items;
            BookList.DataBind();
            ResultSummary.Text = (result.Total == 1 ? "1 title" : result.Total + " titles") +
                (campus.HasValue ? " · stock shown for " + H(Campus.SelectedItem.Text) : " · stock shown across all campuses");
            EmptyState.Text = result.Total == 0 ? Ui.Empty(EmptyArt.Search, "No textbooks match your search", "Try another title, author or ISBN, or clear the filters.") : "";
            Pager.Text = Ui.Pager(Request, result);
        }

        public static string StockBadge(int available) =>
            available == 0 ? "<span class=\"badge badge--danger\">Out of stock</span>"
            : available <= 2 ? "<span class=\"badge badge--pending\">" + available + " left</span>"
            : "<span class=\"badge badge--success\">" + available + " available</span>";
    }
}
