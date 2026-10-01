using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using StudyLink.Data;
using StudyLink.Data.Entities;
using StudyLink.Data.Seed;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Interfaces;
using StudyLink.Services.Policies;
using StudyLink.Services.Security;
using StudyLink.Services.Validation;

namespace StudyLink.Services.Implementations
{
    internal static class BookRules
    {
        /// <summary>Rental fee for one rental period (SC12: weekly fee × weeks in the period).</summary>
        public static decimal RentalFee(decimal weeklyFee, int periodDays) =>
            MoneyPolicy.Round(weeklyFee * Math.Max(1, (int)Math.Ceiling(periodDays / 7m)));

        /// <summary>Keeps digits (and a final X for ISBN-10); validates the check digit.</summary>
        public static string NormaliseIsbn(string raw, out string error)
        {
            error = null;
            var s = new string((raw ?? string.Empty).ToUpperInvariant().Where(c => char.IsDigit(c) || c == 'X').ToArray());
            if (s.Length == 13 && s.All(char.IsDigit))
            {
                var sum = 0;
                for (var i = 0; i < 12; i++) sum += (s[i] - '0') * (i % 2 == 0 ? 1 : 3);
                if ((10 - sum % 10) % 10 != s[12] - '0') error = "The ISBN-13 check digit is wrong.";
                return s;
            }
            if (s.Length == 10 && s.Take(9).All(char.IsDigit))
            {
                var sum = 0;
                for (var i = 0; i < 10; i++) sum += (s[i] == 'X' ? 10 : s[i] - '0') * (10 - i);
                if (sum % 11 != 0) error = "The ISBN-10 check digit is wrong.";
                return s;
            }
            error = "Enter a 10- or 13-digit ISBN.";
            return s;
        }

        public static IQueryable<RentalRow> Rows(StudyLinkDbContext db, IQueryable<Rental> q) =>
            q.Select(r => new RentalRow
            {
                RentalId = r.RentalId, Reference = r.Reference, BookId = r.Copy.BookId, BookTitle = r.Copy.Book.Title,
                Barcode = r.Copy.Barcode, Campus = r.Copy.Campus.Name, StudentId = r.StudentId,
                StudentName = r.Student.User.FirstName + " " + r.Student.User.LastName, StudentNumber = r.Student.StudentNumber,
                RentedAt = r.RentedAt, DueDate = r.DueDate, ReturnedAt = r.ReturnedAt, Status = r.Status,
                RenewalCount = r.RenewalCount, Fee = r.Fee, LateFeePerDay = r.Copy.Book.LateFeePerDay, SalePrice = r.Copy.Book.SalePrice,
                HoldExpiresAt = r.HoldExpiresAt,
                FinesOutstanding = db.Fines.Where(f => f.RentalId == r.RentalId && f.Status == FineStatus.Outstanding)
                    .Select(f => (decimal?)f.Amount).Sum() ?? 0
            });

        /// <summary>Adds the derived overdue figures that SQL cannot compute against "today" cleanly.</summary>
        public static RentalRow WithOverdue(RentalRow r, DateTime today)
        {
            r.IsOverdue = RentalPolicy.IsOverdue(r.Status, r.DueDate, today);
            r.DaysOverdue = r.IsOverdue ? RentalPolicy.DaysLate(r.DueDate, today) : 0;
            r.AccruedFine = r.IsOverdue ? RentalPolicy.OverdueFine(r.DaysOverdue, r.LateFeePerDay, r.SalePrice) : 0;
            return r;
        }
    }

    public sealed class CatalogueService : ICatalogueService
    {
        private readonly StudyLinkDbContext _db;
        private readonly ISettingsService _settings;
        private readonly IClock _clock;

        public CatalogueService(StudyLinkDbContext db, ISettingsService settings, IClock clock)
        {
            _db = db;
            _settings = settings;
            _clock = clock;
        }

        public PagedResult<BookCard> Search(BookFilter f)
        {
            var q = _db.Books.AsNoTracking().Where(b => f.IncludeInactive || b.IsActive);
            if (!string.IsNullOrWhiteSpace(f.Text))
            {
                var t = f.Text.Trim();
                q = q.Where(b => b.Title.Contains(t) || b.Authors.Contains(t) || b.Isbn.Contains(t)
                                 || (b.Subject != null && b.Subject.Name.Contains(t)));
            }
            if (f.CategoryId.HasValue) q = q.Where(b => b.CategoryId == f.CategoryId.Value);
            if (f.SubjectId.HasValue) q = q.Where(b => b.SubjectId == f.SubjectId.Value);
            var campus = f.CampusId;
            if (f.AvailableOnly)
                q = q.Where(b => b.Copies.Any(c => c.Status == CopyStatus.Available && (!campus.HasValue || c.CampusId == campus.Value)));

            var total = q.Count();
            var page = Math.Max(1, f.Page);
            var cards = q.OrderBy(b => b.Title).Skip((page - 1) * f.PageSize).Take(f.PageSize)
                .Select(b => new BookCard
                {
                    BookId = b.BookId, Title = b.Title, Authors = b.Authors, Edition = b.Edition, Isbn = b.Isbn,
                    Category = b.Category.Name, HasCover = b.CoverImage != null, WeeklyRentalFee = b.WeeklyRentalFee,
                    SalePrice = b.SalePrice, IsRentable = b.IsRentable, IsForSale = b.IsForSale, IsActive = b.IsActive,
                    Available = b.Copies.Count(c => c.Status == CopyStatus.Available && (!campus.HasValue || c.CampusId == campus.Value))
                }).ToList();
            return new PagedResult<BookCard>(cards, total, page, f.PageSize);
        }

        public BookDetail Detail(int bookId, int? campusId, Actor viewer)
        {
            var b = _db.Books.AsNoTracking().Include(x => x.Category).Include(x => x.Subject).FirstOrDefault(x => x.BookId == bookId);
            if (b == null || (!b.IsActive && !RolePermissions.Has(viewer, Permission.ManageBooks))) return null;

            var period = _settings.GetInt(SettingKeys.RentalPeriodDays, 7);
            var stock = _db.Campuses.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.CampusId).Select(c => new CampusStock
            {
                CampusId = c.CampusId, Campus = c.Name,
                Available = _db.BookCopies.Count(x => x.BookId == bookId && x.CampusId == c.CampusId && x.Status == CopyStatus.Available)
            }).ToList();

            var detail = new BookDetail
            {
                BookId = b.BookId, Isbn = b.Isbn, Title = b.Title, Authors = b.Authors, Edition = b.Edition, Publisher = b.Publisher,
                PublishedYear = b.PublishedYear, Category = b.Category.Name, Subject = b.Subject?.Name, Description = b.Description,
                HasCover = b.CoverImage != null, SalePrice = b.SalePrice, WeeklyRentalFee = b.WeeklyRentalFee,
                RentalFee = BookRules.RentalFee(b.WeeklyRentalFee, period), LateFeePerDay = b.LateFeePerDay,
                IsRentable = b.IsRentable, IsForSale = b.IsForSale, IsActive = b.IsActive, RentalPeriodDays = period,
                DueDateIfRentedToday = RentalPolicy.DueDate(_clock.Today, period), CampusId = campusId, Stock = stock,
                AvailableAtCampus = campusId.HasValue ? stock.Where(s => s.CampusId == campusId.Value).Select(s => s.Available).FirstOrDefault()
                                                      : stock.Sum(s => s.Available)
            };

            var studentId = viewer == null ? null : Profiles.StudentId(_db, viewer);
            if (studentId.HasValue)
            {
                detail.OnWaitlist = _db.BookWaitlists.Any(w => w.BookId == bookId && w.StudentId == studentId && w.IsActive
                                                               && (!campusId.HasValue || w.CampusId == campusId.Value));
                var current = _db.Rentals.Where(r => r.StudentId == studentId && r.Copy.BookId == bookId
                                                     && (r.Status == RentalStatus.Active || r.Status == RentalStatus.PendingPayment))
                    .Select(r => r.Reference).FirstOrDefault();
                if (current != null) detail.AlreadyHas = "You are already renting this title (" + current + ").";
            }
            return detail;
        }

        public ServiceResult JoinWaitlist(Actor actor, int bookId, int campusId)
        {
            RolePermissions.Demand(actor, Permission.RentOrBuyBooks);
            var studentId = Profiles.RequireStudent(_db, actor);
            if (!_db.Books.Any(b => b.BookId == bookId && b.IsActive)) return ServiceResult.Fail("Book not found.");
            if (!_db.Campuses.Any(c => c.CampusId == campusId && c.IsActive)) return ServiceResult.Fail("Choose a campus.");
            if (_db.BookCopies.Any(c => c.BookId == bookId && c.CampusId == campusId && c.Status == CopyStatus.Available))
                return ServiceResult.Fail("This title is available at that campus now - you can rent it straight away.");

            var existing = _db.BookWaitlists.FirstOrDefault(w => w.BookId == bookId && w.StudentId == studentId && w.CampusId == campusId);
            if (existing != null)
            {
                if (existing.IsActive) return ServiceResult.Fail("You are already on the waitlist for this title.");
                existing.IsActive = true;
                existing.CreatedAt = _clock.Now;
                existing.NotifiedAt = null;
            }
            else
            {
                _db.BookWaitlists.Add(new BookWaitlist { BookId = bookId, StudentId = studentId, CampusId = campusId, CreatedAt = _clock.Now });
            }
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult LeaveWaitlist(Actor actor, int waitlistId)
        {
            var studentId = Profiles.RequireStudent(_db, actor);
            var w = _db.BookWaitlists.FirstOrDefault(x => x.WaitlistId == waitlistId && x.StudentId == studentId);
            if (w == null) return ServiceResult.Fail("Not found.");
            w.IsActive = false;
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public IList<WaitlistRow> MyWaitlist(Actor actor)
        {
            var studentId = Profiles.RequireStudent(_db, actor);
            return _db.BookWaitlists.AsNoTracking().Where(w => w.StudentId == studentId && w.IsActive)
                .OrderBy(w => w.CreatedAt).Select(w => new WaitlistRow
                {
                    WaitlistId = w.WaitlistId, BookId = w.BookId, BookTitle = w.Book.Title, Campus = w.Campus.Name,
                    CreatedAt = w.CreatedAt, NotifiedAt = w.NotifiedAt
                }).ToList();
        }

        public byte[] Cover(int bookId, out string contentType)
        {
            var c = _db.Books.AsNoTracking().Where(b => b.BookId == bookId).Select(b => new { b.CoverImage, b.CoverContentType }).FirstOrDefault();
            contentType = c?.CoverContentType;
            return c?.CoverImage;
        }
    }

    public sealed class InventoryService : IInventoryService
    {
        private static readonly string[] CoverTypes = { "image/jpeg", "image/png", "image/webp" };

        private readonly StudyLinkDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IClock _clock;

        public InventoryService(StudyLinkDbContext db, IAuditService audit, INotificationService notifications, IClock clock)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
            _clock = clock;
        }

        public PagedResult<InventoryRow> Inventory(Actor actor, BookFilter f)
        {
            if (!RolePermissions.Has(actor, Permission.ManageBooks) && !RolePermissions.Has(actor, Permission.ViewOperationalReports))
                throw new AccessDeniedException("You cannot view inventory.");

            var today = _clock.Today;
            var q = _db.Books.AsNoTracking().Where(b => f.IncludeInactive || b.IsActive);
            if (!string.IsNullOrWhiteSpace(f.Text))
            {
                var t = f.Text.Trim();
                q = q.Where(b => b.Title.Contains(t) || b.Isbn.Contains(t) || b.Authors.Contains(t));
            }
            if (f.CategoryId.HasValue) q = q.Where(b => b.CategoryId == f.CategoryId.Value);
            var campus = f.CampusId;

            var raw = q.OrderBy(b => b.Title).Select(b => new
            {
                b.BookId, b.Title, b.Isbn, Category = b.Category.Name, b.SalePrice, b.WeeklyRentalFee, b.IsActive, b.ReorderLevel,
                Supplier = b.PreferredSupplier == null ? null : b.PreferredSupplier.CompanyName,
                Statuses = b.Copies.Where(c => !campus.HasValue || c.CampusId == campus.Value).Select(c => c.Status),
                Overdue = _db.Rentals.Count(r => r.Copy.BookId == b.BookId && r.Status == RentalStatus.Active && r.DueDate < today
                                                 && (!campus.HasValue || r.Copy.CampusId == campus.Value))
            }).ToList();

            var rows = raw.Select(b => new InventoryRow
            {
                BookId = b.BookId, Title = b.Title, Isbn = b.Isbn, Category = b.Category, SalePrice = b.SalePrice,
                WeeklyRentalFee = b.WeeklyRentalFee, IsActive = b.IsActive, PreferredSupplier = b.Supplier,
                Stock = StockSummary.From(b.Statuses, b.Overdue, b.ReorderLevel)
            }).Where(r => !f.LowStockOnly || (r.IsActive && r.Stock.IsLowStock)).ToList();

            var page = Math.Max(1, f.Page);
            return new PagedResult<InventoryRow>(rows.Skip((page - 1) * f.PageSize).Take(f.PageSize).ToList(), rows.Count, page, f.PageSize);
        }

        public BookEdit Get(Actor actor, int bookId)
        {
            RolePermissions.Demand(actor, Permission.ManageBooks);
            return _db.Books.AsNoTracking().Where(b => b.BookId == bookId).Select(b => new BookEdit
            {
                BookId = b.BookId, Isbn = b.Isbn, Title = b.Title, Authors = b.Authors, Edition = b.Edition, Publisher = b.Publisher,
                PublishedYear = b.PublishedYear, CategoryId = b.CategoryId, SubjectId = b.SubjectId, Description = b.Description,
                SalePrice = b.SalePrice, WeeklyRentalFee = b.WeeklyRentalFee, LateFeePerDay = b.LateFeePerDay,
                ReorderLevel = b.ReorderLevel, IsRentable = b.IsRentable, IsForSale = b.IsForSale, IsActive = b.IsActive,
                PreferredSupplierId = b.PreferredSupplierId, HasCover = b.CoverImage != null
            }).FirstOrDefault();
        }

        public ServiceResult<int> Save(Actor actor, BookEdit e)
        {
            RolePermissions.Demand(actor, Permission.ManageBooks);
            var isbn = BookRules.NormaliseIsbn(e.Isbn, out var isbnError);
            var error = isbnError
                        ?? (string.IsNullOrWhiteSpace(e.Title) || e.Title.Trim().Length > 200 ? "Enter the title (200 characters max)." : null)
                        ?? (string.IsNullOrWhiteSpace(e.Authors) || e.Authors.Trim().Length > 200 ? "Enter the author(s) (200 characters max)." : null)
                        ?? (TextRules.TooLong(e.Edition, 30) || TextRules.TooLong(e.Publisher, 100) ? "Edition or publisher is too long." : null)
                        ?? (TextRules.TooLong(e.Description, 1000) ? "The description is limited to 1000 characters." : null)
                        ?? (e.PublishedYear.HasValue && (e.PublishedYear < 1900 || e.PublishedYear > _clock.Today.Year + 1) ? "Enter a valid publication year." : null)
                        ?? (e.SalePrice < 0 || e.SalePrice > 100000 || e.WeeklyRentalFee < 0 || e.WeeklyRentalFee > 10000
                            || e.LateFeePerDay < 0 || e.LateFeePerDay > 1000 ? "Prices must be positive amounts (sale ≤ R100 000, weekly rental ≤ R10 000, late fee ≤ R1 000)." : null)
                        ?? (e.ReorderLevel < 0 || e.ReorderLevel > 1000 ? "Reorder level must be between 0 and 1000." : null)
                        ?? (!e.IsRentable && !e.IsForSale ? "A book must be available to rent, to buy, or both." : null)
                        ?? (!_db.Categories.Any(c => c.CategoryId == e.CategoryId) ? "Choose a category." : null)
                        ?? (e.SubjectId.HasValue && !_db.Subjects.Any(s => s.SubjectId == e.SubjectId) ? "Choose a valid subject." : null)
                        ?? (e.PreferredSupplierId.HasValue && !_db.Suppliers.Any(s => s.SupplierId == e.PreferredSupplierId) ? "Choose a valid supplier." : null)
                        ?? (e.CoverImage != null && (e.CoverImage.Length > 2 * 1024 * 1024 || !CoverTypes.Contains(e.CoverContentType))
                            ? "Covers must be JPEG, PNG or WebP images under 2 MB." : null)
                        ?? (_db.Books.Any(b => b.Isbn == isbn && b.BookId != e.BookId) ? "A book with this ISBN already exists." : null);
            if (error != null) return ServiceResult<int>.Fail(error);

            var now = _clock.Now;
            var managerId = _db.Managers.Where(m => m.UserId == actor.UserId).Select(m => (int?)m.ManagerId).FirstOrDefault();
            Book book;
            string before = null;
            if (e.BookId.HasValue)
            {
                book = _db.Books.Find(e.BookId.Value);
                if (book == null) return ServiceResult<int>.Fail("Book not found.");
                before = $"Sale R{book.SalePrice:0.00}, rent R{book.WeeklyRentalFee:0.00}/wk, late R{book.LateFeePerDay:0.00}/day";
                book.UpdatedAt = now;
                book.UpdatedByManagerId = managerId;
            }
            else
            {
                book = _db.Books.Add(new Book { CreatedAt = now, CreatedByManagerId = managerId });
            }

            book.Isbn = isbn;
            book.Title = e.Title.Trim();
            book.Authors = e.Authors.Trim();
            book.Edition = TextRules.Clean(e.Edition, 30);
            book.Publisher = TextRules.Clean(e.Publisher, 100);
            book.PublishedYear = e.PublishedYear;
            book.CategoryId = e.CategoryId;
            book.SubjectId = e.SubjectId;
            book.Description = TextRules.Clean(e.Description, 1000);
            book.SalePrice = MoneyPolicy.Round(e.SalePrice);
            book.WeeklyRentalFee = MoneyPolicy.Round(e.WeeklyRentalFee);
            book.LateFeePerDay = MoneyPolicy.Round(e.LateFeePerDay);
            book.ReorderLevel = e.ReorderLevel;
            book.IsRentable = e.IsRentable;
            book.IsForSale = e.IsForSale;
            book.IsActive = e.IsActive;
            book.PreferredSupplierId = e.PreferredSupplierId;
            if (e.CoverImage != null)
            {
                book.CoverImage = e.CoverImage;
                book.CoverContentType = e.CoverContentType;
            }

            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                return ServiceResult<int>.Fail("A book with this ISBN already exists.");
            }
            _audit.Record(actor, e.BookId.HasValue ? "BookUpdated" : "BookCreated", "Book", book.BookId, before,
                $"Sale R{book.SalePrice:0.00}, rent R{book.WeeklyRentalFee:0.00}/wk, late R{book.LateFeePerDay:0.00}/day", book.Title);
            _db.SaveChanges();
            return ServiceResult<int>.Ok(book.BookId);
        }

        public ServiceResult SetActive(Actor actor, int bookId, bool active)
        {
            RolePermissions.Demand(actor, Permission.ManageBooks);
            var book = _db.Books.Find(bookId);
            if (book == null) return ServiceResult.Fail("Book not found.");
            book.IsActive = active;
            book.UpdatedAt = _clock.Now;
            _audit.Record(actor, active ? "BookActivated" : "BookDeactivated", "Book", bookId, details: book.Title);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public IList<CopyRow> Copies(Actor actor, int bookId)
        {
            if (!RolePermissions.Has(actor, Permission.ManageBooks) && !RolePermissions.Has(actor, Permission.ProcessReturns))
                throw new AccessDeniedException("You cannot view copies.");
            return _db.BookCopies.AsNoTracking().Where(c => c.BookId == bookId).OrderBy(c => c.Campus.Name).ThenBy(c => c.Barcode)
                .Select(c => new CopyRow
                {
                    CopyId = c.CopyId, Barcode = c.Barcode, CampusId = c.CampusId, Campus = c.Campus.Name, Condition = c.Condition,
                    Status = c.Status, AcquiredAt = c.AcquiredAt, Notes = c.Notes,
                    Source = c.DeliveryLine == null ? "Manual" : "Delivery " + c.DeliveryLine.Delivery.Reference,
                    Holder = _db.Rentals.Where(r => r.CopyId == c.CopyId && (r.Status == RentalStatus.Active || r.Status == RentalStatus.PendingPayment))
                        .Select(r => r.Student.User.FirstName + " " + r.Student.User.LastName + " (" + r.Student.StudentNumber + ")").FirstOrDefault(),
                    DueDate = _db.Rentals.Where(r => r.CopyId == c.CopyId && r.Status == RentalStatus.Active).Select(r => (DateTime?)r.DueDate).FirstOrDefault()
                }).ToList();
        }

        public ServiceResult<int> AddCopies(Actor actor, int bookId, int campusId, int quantity, CopyCondition condition, string notes)
        {
            RolePermissions.Demand(actor, Permission.ManageBooks);
            if (quantity < 1 || quantity > 100) return ServiceResult<int>.Fail("Add between 1 and 100 copies at a time.");
            if (!_db.Books.Any(b => b.BookId == bookId)) return ServiceResult<int>.Fail("Book not found.");
            if (!_db.Campuses.Any(c => c.CampusId == campusId)) return ServiceResult<int>.Fail("Choose a campus.");
            if (condition == CopyCondition.Damaged) return ServiceResult<int>.Fail("New stock cannot be added as damaged.");

            var now = _clock.Now;
            CopyFactory.Create(_db, bookId, campusId, quantity, condition, now, TextRules.Clean(notes, 200), null);
            _audit.Record(actor, "StockAdded", "Book", bookId, newState: "+" + quantity + " copies",
                details: _db.Campuses.Where(c => c.CampusId == campusId).Select(c => c.Name).Single());
            _db.SaveChanges();
            NotifyWaitlist(_db, _notifications, bookId, campusId, now);
            _db.SaveChanges();
            return ServiceResult<int>.Ok(quantity);
        }

        public ServiceResult UpdateCopy(Actor actor, int copyId, CopyStatus status, CopyCondition condition, string notes)
        {
            RolePermissions.Demand(actor, Permission.ManageBooks);
            var copy = _db.BookCopies.Find(copyId);
            if (copy == null) return ServiceResult.Fail("Copy not found.");
            // Rented, reserved and sold copies change state only through rentals, returns and sales.
            var managed = new[] { CopyStatus.Available, CopyStatus.Damaged, CopyStatus.Lost, CopyStatus.WrittenOff };
            if (!managed.Contains(copy.Status) || !managed.Contains(status))
                return ServiceResult.Fail("This copy is out on rental, reserved or sold. Use returns to change it.");
            if (copy.Status == CopyStatus.WrittenOff && status != CopyStatus.WrittenOff)
                return ServiceResult.Fail("A written-off copy cannot be brought back into stock.");
            if (status == CopyStatus.Available && condition == CopyCondition.Damaged)
                return ServiceResult.Fail("Repair the copy (set its condition) before making it available.");

            var before = copy.Status + "/" + copy.Condition;
            copy.Status = status;
            copy.Condition = condition;
            copy.Notes = TextRules.Clean(notes, 200) ?? copy.Notes;
            _audit.Record(actor, "StockChanged", "BookCopy", copy.Barcode, before, status + "/" + condition, copy.Notes);
            _db.SaveChanges();
            if (status == CopyStatus.Available)
            {
                NotifyWaitlist(_db, _notifications, copy.BookId, copy.CampusId, _clock.Now);
                _db.SaveChanges();
            }
            return ServiceResult.Ok();
        }

        public IList<CategoryRow> Categories(Actor actor)
        {
            RolePermissions.Demand(actor, Permission.ManageBooks);
            return _db.Categories.AsNoTracking().OrderBy(c => c.Name).Select(c => new CategoryRow
            {
                CategoryId = c.CategoryId, Name = c.Name, Description = c.Description, IsActive = c.IsActive,
                Books = _db.Books.Count(b => b.CategoryId == c.CategoryId)
            }).ToList();
        }

        public ServiceResult SaveCategory(Actor actor, int? categoryId, string name, string description)
        {
            RolePermissions.Demand(actor, Permission.ManageBooks);
            name = (name ?? string.Empty).Trim();
            if (name.Length < 2 || name.Length > 80) return ServiceResult.Fail("Category names are 2–80 characters.");
            if (TextRules.TooLong(description, 300)) return ServiceResult.Fail("The description is limited to 300 characters.");
            if (_db.Categories.Any(c => c.Name == name && c.CategoryId != categoryId)) return ServiceResult.Fail("That category already exists.");
            var category = categoryId.HasValue ? _db.Categories.Find(categoryId.Value) : _db.Categories.Add(new Category { IsActive = true });
            if (category == null) return ServiceResult.Fail("Category not found.");
            category.Name = name;
            category.Description = TextRules.Clean(description, 300);
            _db.SaveChanges();
            _audit.Record(actor, categoryId.HasValue ? "CategoryUpdated" : "CategoryCreated", "Category", category.CategoryId, newState: name);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult SetCategoryActive(Actor actor, int categoryId, bool active)
        {
            RolePermissions.Demand(actor, Permission.ManageBooks);
            var category = _db.Categories.Find(categoryId);
            if (category == null) return ServiceResult.Fail("Category not found.");
            category.IsActive = active;
            _audit.Record(actor, active ? "CategoryActivated" : "CategoryDeactivated", "Category", categoryId);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        /// <summary>When stock returns to a campus, tell the student who has waited longest (SC11 waitlist).</summary>
        internal static bool NotifyWaitlist(StudyLinkDbContext db, INotificationService notifications, int bookId, int campusId, DateTime now)
        {
            var next = db.BookWaitlists.Include(w => w.Student).Include(w => w.Book)
                .Where(w => w.BookId == bookId && w.CampusId == campusId && w.IsActive && w.NotifiedAt == null)
                .OrderBy(w => w.CreatedAt).FirstOrDefault();
            if (next == null) return false;
            next.NotifiedAt = now;
            notifications.Notify(next.Student.UserId, NotificationType.WaitlistAvailable, "A book you wanted is available",
                "\"" + next.Book.Title + "\" is back in stock. Reserve it before someone else does.",
                "~/Catalogue/BookDetails.aspx?id=" + bookId + "&campus=" + campusId);
            return true;
        }
    }

    /// <summary>Creates copies with unique, readable barcodes: SL-000123-7K3Q9.</summary>
    internal static class CopyFactory
    {
        public static IList<BookCopy> Create(StudyLinkDbContext db, int bookId, int campusId, int quantity, CopyCondition condition,
                                             DateTime now, string notes, DeliveryLine source)
        {
            var created = new List<BookCopy>();
            var used = new HashSet<string>();
            for (var i = 0; i < quantity; i++)
            {
                string barcode;
                do
                {
                    barcode = "SL-" + bookId.ToString("D6", CultureInfo.InvariantCulture) + "-" + ReferenceGenerator.RandomCode(5);
                } while (!used.Add(barcode) || db.BookCopies.Any(c => c.Barcode == barcode));

                created.Add(db.BookCopies.Add(new BookCopy
                {
                    BookId = bookId, CampusId = campusId, Barcode = barcode, Condition = condition, Status = CopyStatus.Available,
                    AcquiredAt = now, Notes = notes, DeliveryLine = source
                }));
            }
            return created;
        }
    }

    public sealed class RentalService : IRentalService
    {
        private readonly StudyLinkDbContext _db;
        private readonly ISettingsService _settings;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IClock _clock;

        public RentalService(StudyLinkDbContext db, ISettingsService settings, IAuditService audit,
                             INotificationService notifications, IClock clock)
        {
            _db = db;
            _settings = settings;
            _audit = audit;
            _notifications = notifications;
            _clock = clock;
        }

        public ServiceResult<int> Reserve(Actor actor, int bookId, int campusId) => Take(actor, bookId, campusId, rent: true);

        public ServiceResult<int> Buy(Actor actor, int bookId, int campusId) => Take(actor, bookId, campusId, rent: false);

        /// <summary>
        /// Claims one available copy. UPDLOCK + READPAST (under READ COMMITTED) lets two students
        /// grab two different copies at once, and never the same one; the filtered unique indexes on
        /// Rentals/Sales are the backstop. Stock decrements here (Available → Reserved).
        /// </summary>
        private ServiceResult<int> Take(Actor actor, int bookId, int campusId, bool rent)
        {
            RolePermissions.Demand(actor, Permission.RentOrBuyBooks);
            var studentId = Profiles.RequireStudent(_db, actor);
            var book = _db.Books.Find(bookId);
            if (book == null || !book.IsActive) return ServiceResult<int>.Fail("This book is not available.");
            if (rent && !book.IsRentable) return ServiceResult<int>.Fail("This title is for sale only.");
            if (!rent && !book.IsForSale) return ServiceResult<int>.Fail("This title is for rental only.");
            if (!_db.Campuses.Any(c => c.CampusId == campusId && c.IsActive)) return ServiceResult<int>.Fail("Choose a campus.");

            var today = _clock.Today;
            if (rent)
            {
                if (_db.Rentals.Any(r => r.StudentId == studentId && r.Status == RentalStatus.Active && r.DueDate < today))
                    return ServiceResult<int>.Fail("You have an overdue book. Return it before renting another.");
                if (_db.Rentals.Any(r => r.StudentId == studentId && r.Copy.BookId == bookId
                                         && (r.Status == RentalStatus.Active || r.Status == RentalStatus.PendingPayment)))
                    return ServiceResult<int>.Fail("You are already renting this title.");
            }

            var now = _clock.Now;
            var period = _settings.GetInt(SettingKeys.RentalPeriodDays, 7);
            var hold = now.AddMinutes(_settings.GetInt(SettingKeys.RentalHoldMinutes, 30));
            try
            {
                using (var tx = _db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    var copyId = _db.Database.SqlQuery<int>(
                        "SELECT TOP (1) CopyId FROM dbo.BookCopies WITH (UPDLOCK, READPAST, ROWLOCK) " +
                        "WHERE BookId = @p0 AND CampusId = @p1 AND Status = @p2 ORDER BY Condition, CopyId",
                        bookId, campusId, (int)CopyStatus.Available).FirstOrDefault();
                    if (copyId == 0)
                        return ServiceResult<int>.Fail("Sorry, no copies are available at this campus right now. Join the waitlist to be told when one is returned.");

                    var copy = _db.BookCopies.Find(copyId);
                    copy.Status = CopyStatus.Reserved;
                    var campus = _db.Campuses.Where(c => c.CampusId == campusId).Select(c => c.Name).Single();
                    int id;
                    if (rent)
                    {
                        var fee = BookRules.RentalFee(book.WeeklyRentalFee, period);
                        var rental = _db.Rentals.Add(new Rental
                        {
                            Reference = ReferenceGenerator.New("RN", now), StudentId = studentId, Copy = copy, RentedAt = now,
                            DueDate = RentalPolicy.DueDate(now, period), Fee = fee, CreatedAt = now,
                            Status = fee > 0 ? RentalStatus.PendingPayment : RentalStatus.Active,
                            HoldExpiresAt = fee > 0 ? hold : (DateTime?)null
                        });
                        if (fee > 0)
                            FeeLedger.Add(_db, studentId, FeeType.RentalFee, fee, "Textbook rental – \"" + book.Title + "\" (" + period + " days, " + campus + ")", now, rental: rental);
                        else
                            copy.Status = CopyStatus.Rented;
                        _db.SaveChanges();
                        id = rental.RentalId;
                        _audit.Record(actor, "RentalCreated", "Rental", rental.Reference, newState: copy.Barcode);
                    }
                    else
                    {
                        var sale = _db.Sales.Add(new Sale
                        {
                            Reference = ReferenceGenerator.New("SL", now), StudentId = studentId, Copy = copy, Price = book.SalePrice,
                            CreatedAt = now, Status = book.SalePrice > 0 ? SaleStatus.PendingPayment : SaleStatus.Completed,
                            HoldExpiresAt = book.SalePrice > 0 ? hold : (DateTime?)null,
                            CompletedAt = book.SalePrice > 0 ? (DateTime?)null : now
                        });
                        if (book.SalePrice > 0)
                            FeeLedger.Add(_db, studentId, FeeType.BookPurchase, book.SalePrice, "Book purchase – \"" + book.Title + "\" (" + campus + ")", now, sale: sale);
                        else
                            copy.Status = CopyStatus.Sold;
                        _db.SaveChanges();
                        id = sale.SaleId;
                        _audit.Record(actor, "SaleCreated", "Sale", sale.Reference, newState: copy.Barcode);
                    }
                    _db.SaveChanges();
                    tx.Commit();
                    return ServiceResult<int>.Ok(id);
                }
            }
            catch (Exception ex) when (DbErrors.IsConcurrencyConflict(ex))
            {
                return ServiceResult<int>.Fail("That copy was just taken by someone else. Please try again.");
            }
        }

        public IList<RentalRow> MyRentals(Actor actor)
        {
            var studentId = Profiles.RequireStudent(_db, actor);
            var today = _clock.Today;
            return BookRules.Rows(_db, _db.Rentals.AsNoTracking().Where(r => r.StudentId == studentId && r.Status != RentalStatus.Cancelled))
                .OrderByDescending(r => r.RentedAt).ToList().Select(r => BookRules.WithOverdue(r, today)).ToList();
        }

        public IList<SaleRow> MyPurchases(Actor actor)
        {
            var studentId = Profiles.RequireStudent(_db, actor);
            return _db.Sales.AsNoTracking().Where(s => s.StudentId == studentId && s.Status != SaleStatus.Cancelled)
                .OrderByDescending(s => s.CreatedAt).Select(s => new SaleRow
                {
                    SaleId = s.SaleId, Reference = s.Reference, BookTitle = s.Copy.Book.Title, Barcode = s.Copy.Barcode,
                    Price = s.Price, Status = s.Status, CreatedAt = s.CreatedAt, CompletedAt = s.CompletedAt, HoldExpiresAt = s.HoldExpiresAt
                }).ToList();
        }

        public RentalRow Get(Actor actor, int rentalId)
        {
            var row = BookRules.Rows(_db, _db.Rentals.AsNoTracking().Where(r => r.RentalId == rentalId)).FirstOrDefault();
            if (row == null) return null;
            var ownStudent = Profiles.StudentId(_db, actor);
            if (row.StudentId != ownStudent && !RolePermissions.Has(actor, Permission.ProcessReturns)) return null;
            return BookRules.WithOverdue(row, _clock.Today);
        }

        public ServiceResult Renew(Actor actor, int rentalId)
        {
            RolePermissions.Demand(actor, Permission.RentOrBuyBooks);
            var studentId = Profiles.RequireStudent(_db, actor);
            var rental = _db.Rentals.Include(r => r.Copy.Book).FirstOrDefault(r => r.RentalId == rentalId && r.StudentId == studentId);
            if (rental == null) return ServiceResult.Fail("Rental not found.");

            var today = _clock.Today;
            var waiting = _db.BookWaitlists.Count(w => w.BookId == rental.Copy.BookId && w.CampusId == rental.Copy.CampusId && w.IsActive);
            var error = RentalPolicy.CheckRenewal(rental.Status, rental.DueDate, today, rental.RenewalCount,
                _settings.GetInt(SettingKeys.MaxRenewals, 2), waiting);
            if (error != null) return ServiceResult.Fail(error);

            var now = _clock.Now;
            var period = _settings.GetInt(SettingKeys.RentalPeriodDays, 7);
            var fee = BookRules.RentalFee(rental.Copy.Book.WeeklyRentalFee, period);
            var renewal = _db.RentalRenewals.Add(new RentalRenewal
            {
                Rental = rental, RenewedAt = now, PreviousDueDate = rental.DueDate, NewDueDate = rental.DueDate.AddDays(period), Fee = fee
            });
            rental.DueDate = renewal.NewDueDate;
            rental.RenewalCount++;
            if (fee > 0)
                FeeLedger.Add(_db, studentId, FeeType.RenewalFee, fee, "Rental renewal – \"" + rental.Copy.Book.Title + "\" to " + renewal.NewDueDate.ToString("d MMM yyyy"), now, renewal: renewal);
            _audit.Record(actor, "RentalRenewed", "Rental", rental.Reference, renewal.PreviousDueDate.ToString("yyyy-MM-dd"), renewal.NewDueDate.ToString("yyyy-MM-dd"));
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public PagedResult<RentalRow> Search(Actor actor, RentalFilter f)
        {
            if (!RolePermissions.Has(actor, Permission.ProcessReturns))
                throw new AccessDeniedException("You cannot view rentals.");
            var today = _clock.Today;
            var q = _db.Rentals.AsNoTracking().Where(r => r.Status != RentalStatus.Cancelled);
            if (f.Status.HasValue) q = q.Where(r => r.Status == f.Status.Value);
            if (f.OverdueOnly) q = q.Where(r => r.Status == RentalStatus.Active && r.DueDate < today);
            if (f.CampusId.HasValue) q = q.Where(r => r.Copy.CampusId == f.CampusId.Value);
            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var s = f.Search.Trim();
                q = q.Where(r => r.Reference.Contains(s) || r.Copy.Barcode.Contains(s) || r.Copy.Book.Title.Contains(s)
                                 || r.Student.StudentNumber.Contains(s) || r.Student.User.LastName.Contains(s));
            }
            var total = q.Count();
            var page = Math.Max(1, f.Page);
            var rows = BookRules.Rows(_db, q.OrderBy(r => r.Status).ThenBy(r => r.DueDate).Skip((page - 1) * f.PageSize).Take(f.PageSize))
                .ToList().Select(r => BookRules.WithOverdue(r, today)).ToList();
            return new PagedResult<RentalRow>(rows, total, page, f.PageSize);
        }

        public RentalRow FindActiveByBarcode(Actor actor, string barcode)
        {
            RolePermissions.Demand(actor, Permission.ProcessReturns);
            barcode = (barcode ?? string.Empty).Trim().ToUpperInvariant();
            if (barcode.Length == 0) return null;
            var row = BookRules.Rows(_db, _db.Rentals.AsNoTracking().Where(r => r.Status == RentalStatus.Active
                                                                               && (r.Copy.Barcode == barcode || r.Reference == barcode))).FirstOrDefault();
            return row == null ? null : BookRules.WithOverdue(row, _clock.Today);
        }

        public ServiceResult<ReturnResult> ProcessReturn(Actor actor, ReturnRequest request)
        {
            RolePermissions.Demand(actor, Permission.ProcessReturns);
            if (request.DamageFine < 0) return ServiceResult<ReturnResult>.Fail("The damage fine cannot be negative.");
            if (TextRules.TooLong(request.Notes, 200)) return ServiceResult<ReturnResult>.Fail("Notes are limited to 200 characters.");
            if (!Enum.IsDefined(typeof(CopyCondition), request.Condition)) return ServiceResult<ReturnResult>.Fail("Record the book's condition.");

            var rental = _db.Rentals.Include(r => r.Copy.Book).Include(r => r.Student.User).FirstOrDefault(r => r.RentalId == request.RentalId);
            if (rental == null) return ServiceResult<ReturnResult>.Fail("Rental not found.");
            if (rental.Status != RentalStatus.Active) return ServiceResult<ReturnResult>.Fail("This rental is not out on loan.");
            var book = rental.Copy.Book;
            if (request.DamageFine > book.SalePrice && book.SalePrice > 0)
                return ServiceResult<ReturnResult>.Fail("A damage fine cannot exceed the book's price (R " + book.SalePrice.ToString("N2") + ").");
            if (request.DamageFine > 0 && request.Condition != CopyCondition.Damaged && request.Condition != CopyCondition.Poor)
                return ServiceResult<ReturnResult>.Fail("Charge a damage fine only when the condition is Poor or Damaged.");

            var now = _clock.Now;
            var daysLate = RentalPolicy.DaysLate(rental.DueDate, now.Date);
            var overdue = RentalPolicy.OverdueFine(daysLate, book.LateFeePerDay, book.SalePrice);
            var result = new ReturnResult { RentalId = rental.RentalId, Reference = rental.Reference, DaysLate = daysLate, OverdueFine = overdue, DamageFine = MoneyPolicy.Round(request.DamageFine) };

            rental.Status = RentalStatus.Returned;
            rental.ReturnedAt = now;
            rental.ReturnCondition = request.Condition;
            rental.ProcessedByUserId = actor.UserId;
            // Stock increments here: a sound copy goes straight back on the shelf.
            rental.Copy.Condition = request.Condition;
            rental.Copy.Status = request.Condition == CopyCondition.Damaged ? CopyStatus.Damaged : CopyStatus.Available;
            if (!string.IsNullOrWhiteSpace(request.Notes)) rental.Copy.Notes = request.Notes.Trim();

            if (overdue > 0) AddFine(rental, FineType.Overdue, overdue, daysLate, daysLate + " day(s) late", actor, now);
            if (result.DamageFine > 0) AddFine(rental, FineType.Damage, result.DamageFine, 0, TextRules.Clean(request.Notes, 200) ?? "Returned damaged", actor, now);

            _audit.Record(actor, "RentalReturned", "Rental", rental.Reference, "Active", "Returned",
                request.Condition + (result.TotalFines > 0 ? ", fines R" + result.TotalFines.ToString("0.00", CultureInfo.InvariantCulture) : ""));
            _notifications.Notify(rental.Student.UserId, NotificationType.RentalReturned, "Book returned",
                "\"" + book.Title + "\" was returned." + (result.TotalFines > 0 ? " A fine of R " + result.TotalFines.ToString("N2") + " was added to My Fees." : ""),
                "~/Student/Fees/MyFees.aspx");
            _db.SaveChanges();

            if (rental.Copy.Status == CopyStatus.Available)
            {
                result.WaitlistNotified = InventoryService.NotifyWaitlist(_db, _notifications, book.BookId, rental.Copy.CampusId, now);
                _db.SaveChanges();
            }
            return ServiceResult<ReturnResult>.Ok(result);
        }

        public ServiceResult MarkLost(Actor actor, int rentalId, string notes)
        {
            RolePermissions.Demand(actor, Permission.ProcessReturns);
            var rental = _db.Rentals.Include(r => r.Copy.Book).Include(r => r.Student.User).FirstOrDefault(r => r.RentalId == rentalId);
            if (rental == null) return ServiceResult.Fail("Rental not found.");
            if (rental.Status != RentalStatus.Active) return ServiceResult.Fail("Only a book on loan can be marked lost.");

            var now = _clock.Now;
            rental.Status = RentalStatus.Lost;
            rental.ProcessedByUserId = actor.UserId;
            rental.Copy.Status = CopyStatus.Lost;
            var book = rental.Copy.Book;
            if (book.SalePrice > 0)
                AddFine(rental, FineType.Lost, book.SalePrice, RentalPolicy.DaysLate(rental.DueDate, now.Date),
                    TextRules.Clean(notes, 200) ?? "Book reported lost – replacement cost", actor, now);
            _audit.Record(actor, "RentalLost", "Rental", rental.Reference, "Active", "Lost", notes);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        private void AddFine(Rental rental, FineType type, decimal amount, int daysLate, string reason, Actor actor, DateTime now)
        {
            var fine = _db.Fines.Add(new Fine
            {
                Rental = rental, Type = type, Amount = MoneyPolicy.Round(amount), DaysLate = daysLate, Reason = reason,
                CreatedAt = now, CreatedByUserId = actor.IsSystem ? (int?)null : actor.UserId
            });
            var feeType = type == FineType.Overdue ? FeeType.OverdueFine : type == FineType.Damage ? FeeType.DamageFine : FeeType.LostBookFine;
            FeeLedger.Add(_db, rental.StudentId, feeType, fine.Amount,
                (type == FineType.Overdue ? "Overdue fine" : type == FineType.Damage ? "Damage fine" : "Lost book") + " – \"" + rental.Copy.Book.Title + "\"",
                now, fine: fine);
            _notifications.Notify(rental.Student.UserId, NotificationType.FineCreated, "Fine added",
                "A " + type.ToString().ToLowerInvariant() + " fine of R " + fine.Amount.ToString("N2") + " was added for \"" + rental.Copy.Book.Title + "\".",
                "~/Student/Fees/MyFees.aspx");
        }

        public int ExpireHolds()
        {
            var now = _clock.Now;
            var changed = 0;
            foreach (var r in _db.Rentals.Include(x => x.Copy).Where(x => x.Status == RentalStatus.PendingPayment && x.HoldExpiresAt < now).ToList())
            {
                r.Status = RentalStatus.Cancelled;
                r.HoldExpiresAt = null;
                if (r.Copy.Status == CopyStatus.Reserved) r.Copy.Status = CopyStatus.Available;
                FeeLedger.CancelUnpaid(_db, rentalId: r.RentalId);
                changed++;
            }
            foreach (var s in _db.Sales.Include(x => x.Copy).Where(x => x.Status == SaleStatus.PendingPayment && x.HoldExpiresAt < now).ToList())
            {
                s.Status = SaleStatus.Cancelled;
                s.HoldExpiresAt = null;
                if (s.Copy.Status == CopyStatus.Reserved) s.Copy.Status = CopyStatus.Available;
                FeeLedger.CancelUnpaid(_db, saleId: s.SaleId);
                changed++;
            }
            _db.SaveChanges();
            return changed;
        }
    }

    public sealed class FineService : IFineService
    {
        private readonly StudyLinkDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IClock _clock;

        public FineService(StudyLinkDbContext db, IAuditService audit, INotificationService notifications, IClock clock)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
            _clock = clock;
        }

        public IList<FineRow> Search(Actor actor, FineStatus? status, string search)
        {
            RolePermissions.Demand(actor, Permission.ManageFines);
            var q = _db.Fines.AsNoTracking().AsQueryable();
            if (status.HasValue) q = q.Where(f => f.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(f => f.Rental.Reference.Contains(s) || f.Rental.Student.StudentNumber.Contains(s)
                                 || f.Rental.Student.User.LastName.Contains(s) || f.Rental.Copy.Book.Title.Contains(s));
            }
            return q.OrderByDescending(f => f.FineId).Take(300).Select(f => new FineRow
            {
                FineId = f.FineId, RentalId = f.RentalId, RentalReference = f.Rental.Reference, BookTitle = f.Rental.Copy.Book.Title,
                StudentName = f.Rental.Student.User.FirstName + " " + f.Rental.Student.User.LastName,
                StudentNumber = f.Rental.Student.StudentNumber, Type = f.Type, Amount = f.Amount, DaysLate = f.DaysLate,
                Status = f.Status, Reason = f.Reason, CreatedAt = f.CreatedAt, WaiverReason = f.WaiverReason
            }).ToList();
        }

        public ServiceResult Waive(Actor actor, int fineId, string reason)
        {
            RolePermissions.Demand(actor, Permission.ManageFines);
            if (string.IsNullOrWhiteSpace(reason)) return ServiceResult.Fail("Give a reason for waiving the fine.");
            var fine = _db.Fines.Include(f => f.Rental.Student).FirstOrDefault(f => f.FineId == fineId);
            if (fine == null) return ServiceResult.Fail("Fine not found.");
            if (fine.Status != FineStatus.Outstanding) return ServiceResult.Fail("Only an outstanding fine can be waived.");

            fine.Status = FineStatus.Waived;
            fine.WaivedByUserId = actor.UserId;
            fine.WaivedAt = _clock.Now;
            fine.WaiverReason = TextRules.Clean(reason, 200);
            foreach (var fee in _db.Fees.Where(f => f.FineId == fineId && f.Status == FeeStatus.Unpaid))
            {
                fee.Status = FeeStatus.Waived;
                fee.SettledAt = _clock.Now;
            }
            _audit.Record(actor, "FineWaived", "Fine", fineId, "Outstanding", "Waived", fine.WaiverReason);
            _notifications.Notify(fine.Rental.Student.UserId, NotificationType.Administrative, "Fine waived",
                "A fine of R " + fine.Amount.ToString("N2") + " was waived: " + fine.WaiverReason, "~/Student/Fees/MyFees.aspx");
            _db.SaveChanges();
            return ServiceResult.Ok();
        }
    }
}
