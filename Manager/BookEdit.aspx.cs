using System;
using System.Globalization;
using System.IO;
using System.Linq;
using StudyLink.Data.Entities;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Manager
{
    /// <summary>Add or edit a title (SC25) and manage its physical copies per campus.</summary>
    [RequirePermission(Permission.ManageBooks)]
    public partial class BookEdit : BasePage
    {
        protected int BookIdValue => QueryInt("id") ?? 0;
        private bool IsNew => !QueryInt("id").HasValue;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            Ui.Fill(Category, S.Lookups.Categories());
            Ui.Fill(Subject, S.Lookups.Subjects(), "None");
            Ui.Fill(Supplier, S.Lookups.Suppliers(), "None");
            Ui.Fill(CopyCampus, S.Lookups.Campuses());
            Ui.FillEnum<CopyCondition>(CopyCondition);
            Ui.FillEnum<CopyCondition>(EditCondition);
            Ui.FillEnum<CopyStatus>(EditStatus);
            Bind();
        }

        private void Bind()
        {
            if (IsNew)
            {
                Heading = "Add a book";
                Subheading = "New titles appear in the catalogue once copies are in stock.";
                Rentable.Checked = ForSale.Checked = true;
                Reorder.Text = "3";
                CopiesSide.Visible = CopiesPanel.Visible = RetireButton.Visible = RestoreButton.Visible = false;
                return;
            }

            var b = S.Inventory.Get(Actor, BookIdValue);
            if (b == null)
            {
                RedirectWith("~/Manager/Books.aspx", "That book could not be found.", AlertKind.Warning);
                return;
            }
            Heading = b.Title;
            Subheading = b.Authors + " · ISBN " + b.Isbn;
            StatusBadge.Text = Fmt.Badge(b.IsActive ? "Active" : "Inactive");
            BookTitle.Text = b.Title; Isbn.Text = b.Isbn; Authors.Text = b.Authors; Edition.Text = b.Edition; Publisher.Text = b.Publisher;
            Year.Text = b.PublishedYear?.ToString(CultureInfo.InvariantCulture);
            Ui.Select(Category, b.CategoryId.ToString(CultureInfo.InvariantCulture));
            Ui.Select(Subject, b.SubjectId?.ToString(CultureInfo.InvariantCulture) ?? "");
            Ui.Select(Supplier, b.PreferredSupplierId?.ToString(CultureInfo.InvariantCulture) ?? "");
            SalePrice.Text = b.SalePrice.ToString("0.00", CultureInfo.InvariantCulture);
            WeeklyFee.Text = b.WeeklyRentalFee.ToString("0.00", CultureInfo.InvariantCulture);
            LateFee.Text = b.LateFeePerDay.ToString("0.00", CultureInfo.InvariantCulture);
            Reorder.Text = b.ReorderLevel.ToString(CultureInfo.InvariantCulture);
            Description.Text = b.Description;
            Rentable.Checked = b.IsRentable;
            ForSale.Checked = b.IsForSale;
            CoverPreview.Text = "<div class=\"book-cover cover-preview\">" + Ui.BookCover(b.BookId ?? 0, b.HasCover, b.Title, b.Authors, b.Edition, "") + "</div>";
            CoverHint.Text = b.HasCover ? "Upload a new image only to replace the current cover." : "This title has no cover yet; students see a plain placeholder.";
            RetireButton.Visible = b.IsActive;
            RestoreButton.Visible = !b.IsActive;
            BindCopies();
        }

        private void BindCopies()
        {
            var copies = S.Inventory.Copies(Actor, BookIdValue);
            Copies.DataSource = copies;
            Copies.DataBind();
            CopyCount.Text = copies.Count + " cop" + (copies.Count == 1 ? "y" : "ies") + " · " + copies.Count(c => c.Status == CopyStatus.Available) + " available";

            var copy = QueryInt("copy") is int copyId ? copies.FirstOrDefault(c => c.CopyId == copyId) : null;
            CopyEdit.Visible = copy != null;
            if (copy == null) return;
            CopyBarcode.Text = H(copy.Barcode);
            Ui.Select(EditStatus, ((int)copy.Status).ToString(CultureInfo.InvariantCulture));
            Ui.Select(EditCondition, ((int)copy.Condition).ToString(CultureInfo.InvariantCulture));
            EditNotes.Text = copy.Notes;
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            var sale = Ui.Money(SalePrice.Text);
            var weekly = Ui.Money(WeeklyFee.Text);
            var late = Ui.Money(LateFee.Text);
            var reorder = Ui.Int(Reorder.Text);
            if (!sale.HasValue || !weekly.HasValue || !late.HasValue || !reorder.HasValue)
            {
                Fail("Enter the prices, late fee and reorder level as numbers.");
                return;
            }

            var book = IsNew ? new Services.DTOs.BookEdit() : S.Inventory.Get(Actor, BookIdValue);
            book.Title = BookTitle.Text; book.Isbn = Isbn.Text; book.Authors = Authors.Text; book.Edition = Edition.Text; book.Publisher = Publisher.Text;
            book.PublishedYear = Ui.Int(Year.Text);
            book.CategoryId = Ui.Int(Category) ?? 0;
            book.SubjectId = Ui.Int(Subject);
            book.PreferredSupplierId = Ui.Int(Supplier);
            book.SalePrice = sale.Value; book.WeeklyRentalFee = weekly.Value; book.LateFeePerDay = late.Value; book.ReorderLevel = reorder.Value;
            book.Description = Description.Text;
            book.IsRentable = Rentable.Checked;
            book.IsForSale = ForSale.Checked;

            // Type, size and "new titles need a cover" are checked by the service, from the bytes themselves.
            if (Cover.HasFile)
                using (var ms = new MemoryStream())
                {
                    Cover.PostedFile.InputStream.CopyTo(ms);
                    book.CoverImage = ms.ToArray();
                }

            var result = S.Inventory.Save(Actor, book);
            if (Show(result)) RedirectWith("~/Manager/BookEdit.aspx?id=" + result.Value, IsNew ? "Book added. Now add copies or order stock." : "Book saved.");
        }

        protected void Retire_Click(object sender, EventArgs e)
        {
            if (Show(S.Inventory.SetActive(Actor, BookIdValue, false))) RedirectWith("~/Manager/BookEdit.aspx?id=" + BookIdValue, "Title retired.");
        }

        protected void Restore_Click(object sender, EventArgs e)
        {
            if (Show(S.Inventory.SetActive(Actor, BookIdValue, true))) RedirectWith("~/Manager/BookEdit.aspx?id=" + BookIdValue, "Title restored.");
        }

        protected void AddCopies_Click(object sender, EventArgs e)
        {
            var qty = Ui.Int(CopyQty.Text);
            if (!qty.HasValue)
            {
                Fail("Enter how many copies to add.");
                return;
            }
            var result = S.Inventory.AddCopies(Actor, BookIdValue, Ui.Int(CopyCampus) ?? 0, qty.Value,
                Ui.EnumOf<CopyCondition>(CopyCondition) ?? Data.Entities.CopyCondition.New, CopyNotes.Text);
            if (Show(result)) RedirectWith("~/Manager/BookEdit.aspx?id=" + BookIdValue, result.Value + " cop" + (result.Value == 1 ? "y" : "ies") + " added.");
        }

        protected void UpdateCopy_Click(object sender, EventArgs e)
        {
            var result = S.Inventory.UpdateCopy(Actor, QueryInt("copy") ?? 0,
                Ui.EnumOf<CopyStatus>(EditStatus) ?? CopyStatus.Available,
                Ui.EnumOf<CopyCondition>(EditCondition) ?? Data.Entities.CopyCondition.Good, EditNotes.Text);
            if (Show(result)) RedirectWith("~/Manager/BookEdit.aspx?id=" + BookIdValue, "Copy updated.");
        }
    }
}
