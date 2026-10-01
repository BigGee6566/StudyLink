using System;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Manager
{
    /// <summary>
    /// SC25 - Book Management. The default view is the catalogue (prices, supplier); view=inventory
    /// shows stock counted from copy statuses: available, reserved, on loan, overdue, sold, lost/damaged.
    /// </summary>
    [RequirePermission(Permission.ManageBooks)]
    public partial class Books : BasePage
    {
        protected bool InventoryView => Query("view") == "inventory";

        protected void Page_Load(object sender, EventArgs e)
        {
            Heading = InventoryView ? "Inventory" : "Book Management";
            Subheading = InventoryView ? "Stock per title, counted live from every copy's status." : "Titles, prices and preferred suppliers.";
            TabStrip.Text = Ui.Tabs(Request, "view", ("", "Catalogue"), ("inventory", "Inventory"));
            if (IsPostBack) return;

            Ui.Fill(Category, S.Lookups.Categories(false), "All categories", QueryInt("category"));
            Ui.Fill(Campus, S.Lookups.Campuses(), "All campuses", QueryInt("campus"));
            SearchText.Text = Query("q");
            LowOnly.Checked = Query("low") == "1";
            Inactive.Checked = Query("inactive") == "1";

            Head.Text = "<tr><th>Title</th>" + (InventoryView
                ? "<th class=\"col-num\">Available</th><th class=\"col-num\">Reserved</th><th class=\"col-num\">On loan</th><th class=\"col-num\">Overdue</th><th class=\"col-num\">Sold</th><th class=\"col-num\">Lost / damaged</th><th class=\"col-num\">Reorder at</th>"
                : "<th class=\"col-num\">Rental / week</th><th class=\"col-num\">Sale price</th><th>Preferred supplier</th><th class=\"col-num\">In stock</th>") + "<th>Status</th></tr>";

            var result = S.Inventory.Inventory(Actor, new BookFilter
            {
                Text = SearchText.Text,
                CategoryId = Ui.Int(Category),
                CampusId = Ui.Int(Campus),
                LowStockOnly = LowOnly.Checked,
                IncludeInactive = Inactive.Checked,
                Page = QueryInt("page") ?? 1,
                PageSize = 25
            });
            Rows.DataSource = result.Items;
            Rows.DataBind();
            EmptyState.Text = result.Total == 0 ? Ui.Empty("book", "No titles match.") : "";
            Pager.Text = Ui.Pager(Request, result);
        }

        protected void Search_Click(object sender, EventArgs e) =>
            Go("~/Manager/Books.aspx?view=" + Query("view") + "&q=" + Server.UrlEncode(SearchText.Text.Trim()) + "&category=" + Category.SelectedValue +
               "&campus=" + Campus.SelectedValue + (LowOnly.Checked ? "&low=1" : "") + (Inactive.Checked ? "&inactive=1" : ""));

        protected static string PriceCells(object item)
        {
            var r = (InventoryRow)item;
            return "<td data-label=\"Rental / week\" class=\"col-num\">" + Fmt.Money(r.WeeklyRentalFee) + "</td><td data-label=\"Sale price\" class=\"col-num\">" +
                   Fmt.Money(r.SalePrice) + "</td><td data-label=\"Supplier\">" + Fmt.H(r.PreferredSupplier ?? "–") + "</td><td data-label=\"In stock\" class=\"col-num\">" + r.Stock.InStock + "</td>";
        }

        protected static string StockCells(object item)
        {
            var s = ((InventoryRow)item).Stock;
            string Cell(string label, int value) => "<td data-label=\"" + label + "\" class=\"col-num\">" + value + "</td>";
            return Cell("Available", s.Available) + Cell("Reserved", s.Reserved) + Cell("On loan", s.Rented) + Cell("Overdue", s.Overdue) +
                   Cell("Sold", s.Sold) + Cell("Lost / damaged", s.Lost + s.Damaged + s.WrittenOff) + Cell("Reorder at", s.ReorderLevel);
        }

        protected static string StatusOf(object item)
        {
            var r = (InventoryRow)item;
            if (!r.IsActive) return Fmt.Badge("Inactive");
            if (r.Stock.IsOutOfStock) return Fmt.Badge("Out of stock");
            return r.Stock.IsLowStock ? Fmt.Badge("Low") : Fmt.Badge("Available");
        }
    }
}
