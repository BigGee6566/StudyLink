using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using StudyLink.Data;
using StudyLink.Data.Entities;
using StudyLink.Data.Seed;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Interfaces;
using StudyLink.Services.Policies;
using StudyLink.Services.Security;

namespace StudyLink.Services.Implementations
{
    /// <summary>
    /// SC24. Each report is a query over live tables rendered into a DataTable, so the same result
    /// feeds the on-screen grid and the CSV export. Nothing here is estimated or sampled.
    /// </summary>
    public sealed class ReportService : IReportService
    {
        private static readonly CultureInfo Display = CultureInfo.GetCultureInfo("en-GB");

        private static readonly ReportDefinition[] Definitions =
        {
            new ReportDefinition { Key = "revenue", Title = "Revenue & receipts", Description = "Card payments and refunds per day, with income by fee type.", UsesDateRange = true, ManagerOnly = true },
            new ReportDefinition { Key = "outstanding", Title = "Outstanding balances", Description = "Students with unpaid fees, largest first.", ManagerOnly = true },
            new ReportDefinition { Key = "overdue", Title = "Overdue books", Description = "Books past their due date, with the fine accrued so far." },
            new ReportDefinition { Key = "stock", Title = "Stock status", Description = "Copies per title by status: available, reserved, rented, overdue, sold, lost, damaged." },
            new ReportDefinition { Key = "low-stock", Title = "Low stock", Description = "Active titles whose available copies are below their reorder level." },
            new ReportDefinition { Key = "sales", Title = "Book sales", Description = "Completed book sales in the period.", UsesDateRange = true, ManagerOnly = true },
            new ReportDefinition { Key = "rentals", Title = "Rental activity", Description = "Rentals started, returned and renewed per title in the period.", UsesDateRange = true },
            new ReportDefinition { Key = "tutor-utilisation", Title = "Tutor utilisation", Description = "Hours each tutor published, had booked and delivered in the period.", UsesDateRange = true },
            new ReportDefinition { Key = "tutor-earnings", Title = "Tutor earnings", Description = "Delivered sessions, gross fees, hub commission and tutor share.", UsesDateRange = true, ManagerOnly = true },
            new ReportDefinition { Key = "payroll-hours", Title = "Tutor payroll hours", Description = "Approved timesheet hours per tutor (UC11).", UsesDateRange = true },
            new ReportDefinition { Key = "bookings", Title = "Booking statistics", Description = "Tutoring bookings per subject by outcome.", UsesDateRange = true },
            new ReportDefinition { Key = "daily-metrics", Title = "Daily business metrics", Description = "The end-of-day snapshot recorded for each business day.", UsesDateRange = true, ManagerOnly = true }
        };

        private readonly StudyLinkDbContext _db;
        private readonly ISettingsService _settings;
        private readonly IClock _clock;

        public ReportService(StudyLinkDbContext db, ISettingsService settings, IClock clock)
        {
            _db = db;
            _settings = settings;
            _clock = clock;
        }

        public IList<ReportDefinition> Available(Actor actor)
        {
            if (RolePermissions.Has(actor, Permission.ViewAllReports)) return Definitions.ToList();
            if (RolePermissions.Has(actor, Permission.ViewOperationalReports)) return Definitions.Where(d => !d.ManagerOnly).ToList();
            return new List<ReportDefinition>();
        }

        public ReportResult Run(Actor actor, string key, DateTime? from, DateTime? to)
        {
            var def = Available(actor).FirstOrDefault(d => d.Key == key)
                      ?? throw new AccessDeniedException("You cannot run this report.");
            var today = _clock.Today;
            var start = (from ?? today.AddDays(-29)).Date;
            var end = (to ?? today).Date;
            if (end < start) { var t = start; start = end; end = t; }
            var endExclusive = end.AddDays(1);

            var result = new ReportResult { Definition = def, GeneratedAt = _clock.Now };
            if (def.UsesDateRange)
            {
                result.From = start;
                result.To = end;
            }

            switch (key)
            {
                case "revenue": Revenue(result, start, endExclusive); break;
                case "outstanding": Outstanding(result); break;
                case "overdue": Overdue(result, today); break;
                case "stock": Stock(result, today, false); break;
                case "low-stock": Stock(result, today, true); break;
                case "sales": Sales(result, start, endExclusive); break;
                case "rentals": Rentals(result, start, endExclusive); break;
                case "tutor-utilisation": Utilisation(result, start, endExclusive); break;
                case "tutor-earnings": Earnings(result, start, endExclusive); break;
                case "payroll-hours": Payroll(result, start, endExclusive); break;
                case "bookings": Bookings(result, start, endExclusive); break;
                case "daily-metrics": Metrics(result, start, end); break;
            }

            _db.ReportRuns.Add(new ReportRun
            {
                ReportKey = key, GeneratedByUserId = actor.UserId, GeneratedAt = result.GeneratedAt, RowCount = result.Table.Rows.Count,
                Format = "Screen", Parameters = def.UsesDateRange ? start.ToString("yyyy-MM-dd") + " to " + end.ToString("yyyy-MM-dd") : null
            });
            _db.SaveChanges();
            return result;
        }

        private static DataTable Table(params string[] columns)
        {
            var t = new DataTable();
            foreach (var c in columns) t.Columns.Add(c);
            return t;
        }

        private static string R(decimal v) => "R " + v.ToString("N2", Display);
        private static string D(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static void Sum(ReportResult r, string label, string value) => r.Summary.Add(new KeyValuePair<string, string>(label, value));

        private void Revenue(ReportResult r, DateTime from, DateTime to)
        {
            var payments = _db.Payments.AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Completed && p.CreatedAt >= from && p.CreatedAt < to)
                .Select(p => new { p.CreatedAt, p.Type, p.Amount }).ToList();
            r.Table = Table("Date", "Payments", "Charges", "Refunds", "Net");
            foreach (var day in payments.GroupBy(p => p.CreatedAt.Date).OrderBy(g => g.Key))
            {
                var charges = day.Where(p => p.Type == PaymentType.Charge).Sum(p => p.Amount);
                var refunds = day.Where(p => p.Type == PaymentType.Refund).Sum(p => p.Amount);
                r.Table.Rows.Add(D(day.Key), day.Count(p => p.Type == PaymentType.Charge), R(charges), R(refunds), R(charges - refunds));
            }
            var totalCharges = payments.Where(p => p.Type == PaymentType.Charge).Sum(p => p.Amount);
            var totalRefunds = payments.Where(p => p.Type == PaymentType.Refund).Sum(p => p.Amount);
            Sum(r, "Received", R(totalCharges));
            Sum(r, "Refunded", R(totalRefunds));
            Sum(r, "Net revenue", R(totalCharges - totalRefunds));

            var byType = _db.PaymentAllocations.AsNoTracking()
                .Where(a => a.Payment.Status == PaymentStatus.Completed && a.Payment.Type == PaymentType.Charge
                            && a.Payment.CreatedAt >= from && a.Payment.CreatedAt < to)
                .GroupBy(a => a.Fee.Type).Select(g => new { Type = g.Key, Total = g.Sum(a => a.Amount) }).ToList();
            foreach (var t in byType.OrderByDescending(t => t.Total))
                Sum(r, SplitWords(t.Type.ToString()), R(t.Total));
        }

        private void Outstanding(ReportResult r)
        {
            var rows = _db.Fees.AsNoTracking().Where(f => f.Status == FeeStatus.Unpaid)
                .GroupBy(f => new { f.StudentId, f.Student.StudentNumber, f.Student.User.FirstName, f.Student.User.LastName, f.Student.User.Email })
                .Select(g => new { g.Key.StudentNumber, g.Key.FirstName, g.Key.LastName, g.Key.Email, Items = g.Count(), Total = g.Sum(f => f.Amount), Oldest = g.Min(f => f.CreatedAt) })
                .OrderByDescending(x => x.Total).ToList();
            r.Table = Table("Student number", "Student", "Email", "Unpaid items", "Amount", "Oldest item");
            foreach (var x in rows)
                r.Table.Rows.Add(x.StudentNumber, x.FirstName + " " + x.LastName, x.Email, x.Items, R(x.Total), D(x.Oldest));
            Sum(r, "Students owing", rows.Count.ToString(Display));
            Sum(r, "Total outstanding", R(rows.Sum(x => x.Total)));
        }

        private void Overdue(ReportResult r, DateTime today)
        {
            var rows = BookRules.Rows(_db, _db.Rentals.AsNoTracking().Where(x => x.Status == RentalStatus.Active && x.DueDate < today))
                .OrderBy(x => x.DueDate).ToList().Select(x => BookRules.WithOverdue(x, today)).ToList();
            r.Table = Table("Rental", "Title", "Barcode", "Campus", "Student number", "Student", "Due", "Days overdue", "Fine so far");
            foreach (var x in rows)
                r.Table.Rows.Add(x.Reference, x.BookTitle, x.Barcode, x.Campus, x.StudentNumber, x.StudentName, D(x.DueDate), x.DaysOverdue, R(x.AccruedFine));
            Sum(r, "Overdue books", rows.Count.ToString(Display));
            Sum(r, "Fines accruing", R(rows.Sum(x => x.AccruedFine)));
        }

        private void Stock(ReportResult r, DateTime today, bool lowOnly)
        {
            var raw = _db.Books.AsNoTracking().Where(b => !lowOnly || b.IsActive).OrderBy(b => b.Title).Select(b => new
            {
                b.Title, b.Isbn, Category = b.Category.Name, b.ReorderLevel, b.IsActive,
                Statuses = b.Copies.Select(c => c.Status),
                Overdue = _db.Rentals.Count(x => x.Copy.BookId == b.BookId && x.Status == RentalStatus.Active && x.DueDate < today)
            }).ToList();
            r.Table = Table("Title", "ISBN", "Category", "Available", "Reserved", "Rented", "Overdue", "Sold", "Lost", "Damaged", "Reorder level", "Status");
            var shown = 0;
            foreach (var b in raw)
            {
                var s = StockSummary.From(b.Statuses, b.Overdue, b.ReorderLevel);
                if (lowOnly && !s.IsLowStock) continue;
                shown++;
                r.Table.Rows.Add(b.Title, b.Isbn, b.Category, s.Available, s.Reserved, s.Rented, s.Overdue, s.Sold, s.Lost, s.Damaged, s.ReorderLevel,
                    !b.IsActive ? "Inactive" : s.IsOutOfStock ? "Out of stock" : s.IsLowStock ? "Low" : "OK");
            }
            Sum(r, lowOnly ? "Titles below reorder level" : "Titles", shown.ToString(Display));
            if (!lowOnly)
            {
                var all = raw.SelectMany(b => b.Statuses).ToList();
                Sum(r, "Copies available", all.Count(s => s == CopyStatus.Available).ToString(Display));
                Sum(r, "Copies on loan", all.Count(s => s == CopyStatus.Rented).ToString(Display));
                Sum(r, "Copies sold", all.Count(s => s == CopyStatus.Sold).ToString(Display));
            }
        }

        private void Sales(ReportResult r, DateTime from, DateTime to)
        {
            var rows = _db.Sales.AsNoTracking().Where(s => s.Status == SaleStatus.Completed && s.CompletedAt >= from && s.CompletedAt < to)
                .OrderBy(s => s.CompletedAt)
                .Select(s => new { s.CompletedAt, s.Reference, s.Copy.Book.Title, Campus = s.Copy.Campus.Name, s.Student.StudentNumber, s.Price }).ToList();
            r.Table = Table("Date", "Sale", "Title", "Campus", "Student number", "Price");
            foreach (var s in rows) r.Table.Rows.Add(D(s.CompletedAt.Value), s.Reference, s.Title, s.Campus, s.StudentNumber, R(s.Price));
            Sum(r, "Books sold", rows.Count.ToString(Display));
            Sum(r, "Sales value", R(rows.Sum(s => s.Price)));
        }

        private void Rentals(ReportResult r, DateTime from, DateTime to)
        {
            var started = _db.Rentals.AsNoTracking().Where(x => x.RentedAt >= from && x.RentedAt < to && x.Status != RentalStatus.Cancelled && x.Status != RentalStatus.PendingPayment)
                .GroupBy(x => x.Copy.Book.Title).Select(g => new { Title = g.Key, N = g.Count() }).ToList();
            var returned = _db.Rentals.AsNoTracking().Where(x => x.ReturnedAt >= from && x.ReturnedAt < to)
                .GroupBy(x => x.Copy.Book.Title).Select(g => new { Title = g.Key, N = g.Count() }).ToList();
            var renewed = _db.RentalRenewals.AsNoTracking().Where(x => x.RenewedAt >= from && x.RenewedAt < to)
                .GroupBy(x => x.Rental.Copy.Book.Title).Select(g => new { Title = g.Key, N = g.Count() }).ToList();
            r.Table = Table("Title", "Rentals started", "Returned", "Renewals");
            foreach (var title in started.Select(x => x.Title).Union(returned.Select(x => x.Title)).Union(renewed.Select(x => x.Title)).OrderBy(t => t))
                r.Table.Rows.Add(title, started.Where(x => x.Title == title).Sum(x => x.N), returned.Where(x => x.Title == title).Sum(x => x.N), renewed.Where(x => x.Title == title).Sum(x => x.N));
            Sum(r, "Rentals started", started.Sum(x => x.N).ToString(Display));
            Sum(r, "Returned", returned.Sum(x => x.N).ToString(Display));
            Sum(r, "Renewals", renewed.Sum(x => x.N).ToString(Display));
        }

        internal static IList<TutorUtilisation> ComputeUtilisation(StudyLinkDbContext db, DateTime from, DateTime to)
        {
            var slots = db.TutorAvailability.AsNoTracking().Where(a => a.AvailabilityDate >= from && a.AvailabilityDate < to)
                .Select(a => new { a.TutorSubject.TutorId, a.StartTime, a.EndTime }).ToList();
            var booked = new[] { BookingStatus.AwaitingTutor, BookingStatus.Confirmed, BookingStatus.Completed };
            var bookings = db.Bookings.AsNoTracking().Where(b => b.SessionDate >= from && b.SessionDate < to)
                .Select(b => new { b.TutorId, b.Status, b.StartTime, b.EndTime }).ToList();
            var delivered = db.TutoringSessions.AsNoTracking().Where(s => s.SessionDate >= from && s.SessionDate < to)
                .Select(s => new { s.Booking.TutorId, s.HoursDelivered }).ToList();
            var tutors = db.Tutors.AsNoTracking().Where(t => t.VerificationStatus == TutorVerificationStatus.Verified)
                .Select(t => new { t.TutorId, t.User.FirstName, t.User.LastName }).ToList();

            return tutors.Select(t =>
            {
                var published = slots.Where(s => s.TutorId == t.TutorId).Sum(s => SchedulePolicy.HoursBetween(s.StartTime, s.EndTime));
                var mine = bookings.Where(b => b.TutorId == t.TutorId).ToList();
                var bookedHours = mine.Where(b => booked.Contains(b.Status)).Sum(b => SchedulePolicy.HoursBetween(b.StartTime, b.EndTime));
                return new TutorUtilisation
                {
                    TutorId = t.TutorId, Name = t.FirstName + " " + t.LastName, HoursPublished = published, HoursBooked = bookedHours,
                    HoursDelivered = delivered.Where(d => d.TutorId == t.TutorId).Sum(d => d.HoursDelivered),
                    Completed = mine.Count(b => b.Status == BookingStatus.Completed),
                    Cancelled = mine.Count(b => b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.Declined),
                    Percent = SchedulePolicy.UtilisationPercent(bookedHours, published)
                };
            }).OrderByDescending(u => u.Percent).ToList();
        }

        internal sealed class TutorUtilisation
        {
            public int TutorId;
            public string Name;
            public decimal HoursPublished, HoursBooked, HoursDelivered, Percent;
            public int Completed, Cancelled;
        }

        private void Utilisation(ReportResult r, DateTime from, DateTime to)
        {
            var rows = ComputeUtilisation(_db, from, to);
            r.Table = Table("Tutor", "Hours published", "Hours booked", "Hours delivered", "Utilisation %", "Sessions completed", "Cancelled/declined");
            foreach (var u in rows)
                r.Table.Rows.Add(u.Name, u.HoursPublished, u.HoursBooked, u.HoursDelivered, u.Percent.ToString("0.0", Display), u.Completed, u.Cancelled);
            var published = rows.Sum(u => u.HoursPublished);
            Sum(r, "Verified tutors", rows.Count.ToString(Display));
            Sum(r, "Overall utilisation", SchedulePolicy.UtilisationPercent(rows.Sum(u => u.HoursBooked), published).ToString("0.0", Display) + "%");
            Sum(r, "Hours delivered", rows.Sum(u => u.HoursDelivered).ToString("0.##", Display));
        }

        private void Earnings(ReportResult r, DateTime from, DateTime to)
        {
            var commission = _settings.GetDecimal(SettingKeys.TutorCommissionPercent, 20);
            var rows = _db.TutoringSessions.AsNoTracking()
                .Where(s => s.SessionDate >= from && s.SessionDate < to && s.Attendance != AttendanceStatus.TutorAbsent)
                .GroupBy(s => new { s.Booking.TutorId, s.Booking.Tutor.User.FirstName, s.Booking.Tutor.User.LastName })
                .Select(g => new { g.Key.FirstName, g.Key.LastName, Sessions = g.Count(), Hours = g.Sum(s => s.HoursDelivered), Gross = g.Sum(s => s.Booking.Fee) })
                .OrderByDescending(x => x.Gross).ToList();
            r.Table = Table("Tutor", "Sessions", "Hours", "Gross fees", "Commission", "Tutor share");
            foreach (var x in rows)
            {
                var net = MoneyPolicy.TutorShare(x.Gross, commission);
                r.Table.Rows.Add(x.FirstName + " " + x.LastName, x.Sessions, x.Hours, R(x.Gross), R(x.Gross - net), R(net));
            }
            var gross = rows.Sum(x => x.Gross);
            Sum(r, "Commission rate", commission.ToString("0.##", Display) + "%");
            Sum(r, "Gross tutoring fees", R(gross));
            Sum(r, "Paid to tutors", R(rows.Sum(x => MoneyPolicy.TutorShare(x.Gross, commission))));
        }

        private void Payroll(ReportResult r, DateTime from, DateTime to)
        {
            var rows = _db.Timesheets.AsNoTracking().Where(t => t.Status == TimesheetStatus.Approved && t.WeekStart >= from && t.WeekStart < to)
                .GroupBy(t => new { t.TutorId, t.Tutor.User.FirstName, t.Tutor.User.LastName, t.Tutor.HourlyRate })
                .Select(g => new { g.Key.FirstName, g.Key.LastName, g.Key.HourlyRate, Weeks = g.Count(), Hours = g.Sum(t => t.TotalHours) })
                .OrderBy(x => x.LastName).ToList();
            r.Table = Table("Tutor", "Approved weeks", "Approved hours", "Hourly rate");
            foreach (var x in rows) r.Table.Rows.Add(x.FirstName + " " + x.LastName, x.Weeks, x.Hours, R(x.HourlyRate));
            Sum(r, "Approved hours", rows.Sum(x => x.Hours).ToString("0.##", Display));
            Sum(r, "Timesheets awaiting review", _db.Timesheets.Count(t => t.Status == TimesheetStatus.Submitted).ToString(Display));
        }

        private void Bookings(ReportResult r, DateTime from, DateTime to)
        {
            var rows = _db.Bookings.AsNoTracking().Where(b => b.SessionDate >= from && b.SessionDate < to)
                .GroupBy(b => b.TutorSubject.Subject.Name)
                .Select(g => new
                {
                    Subject = g.Key, Total = g.Count(),
                    Confirmed = g.Count(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.AwaitingTutor),
                    Completed = g.Count(b => b.Status == BookingStatus.Completed),
                    Cancelled = g.Count(b => b.Status == BookingStatus.Cancelled),
                    Declined = g.Count(b => b.Status == BookingStatus.Declined),
                    Expired = g.Count(b => b.Status == BookingStatus.Expired)
                }).OrderByDescending(x => x.Total).ToList();
            r.Table = Table("Subject", "Bookings", "Upcoming", "Completed", "Cancelled", "Declined", "Expired unpaid");
            foreach (var x in rows) r.Table.Rows.Add(x.Subject, x.Total, x.Confirmed, x.Completed, x.Cancelled, x.Declined, x.Expired);
            var total = rows.Sum(x => x.Total);
            Sum(r, "Bookings", total.ToString(Display));
            Sum(r, "Completion rate", (total == 0 ? 0 : Math.Round(rows.Sum(x => x.Completed) * 100m / total, 1)).ToString("0.0", Display) + "%");
        }

        private void Metrics(ReportResult r, DateTime from, DateTime to)
        {
            var rows = _db.DailyMetrics.AsNoTracking().Where(m => m.MetricDate >= from && m.MetricDate <= to).OrderBy(m => m.MetricDate).ToList();
            r.Table = Table("Date", "Revenue", "Refunds", "Payments", "Bookings", "Sessions", "Rentals", "Returns", "Overdue", "Books sold", "Fines", "New students", "New tutors");
            foreach (var m in rows)
                r.Table.Rows.Add(D(m.MetricDate), R(m.Revenue), R(m.Refunds), m.PaymentsCount, m.BookingsCreated, m.SessionsCompleted,
                    m.RentalsStarted, m.RentalsReturned, m.OverdueRentals, m.BooksSold, R(m.FinesIssued), m.NewStudents, m.NewTutors);
            Sum(r, "Days recorded", rows.Count.ToString(Display));
            Sum(r, "Revenue", R(rows.Sum(m => m.Revenue - m.Refunds)));
        }

        public string ToCsv(ReportResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", result.Table.Columns.Cast<DataColumn>().Select(c => Csv(c.ColumnName))));
            foreach (DataRow row in result.Table.Rows)
                sb.AppendLine(string.Join(",", row.ItemArray.Select(v => Csv(Convert.ToString(v, CultureInfo.InvariantCulture)))));
            return sb.ToString();
        }

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            // Neutralise spreadsheet formula injection from user-entered text.
            if (value.Length > 0 && "=+-@".IndexOf(value[0]) >= 0 && !decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                value = "'" + value;
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }

        internal static string SplitWords(string pascal) =>
            string.Concat(pascal.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + char.ToLowerInvariant(c) : c.ToString()));
    }

    public sealed class DashboardService : IDashboardService
    {
        private static readonly CultureInfo Display = CultureInfo.GetCultureInfo("en-GB");

        private readonly StudyLinkDbContext _db;
        private readonly IBookingService _bookings;
        private readonly IRentalService _rentals;
        private readonly IFeeService _fees;
        private readonly ITimesheetService _timesheets;
        private readonly IUserManagementService _users;
        private readonly IInventoryService _inventory;
        private readonly ITutorVerificationService _verification;
        private readonly ISupplierOrderService _orders;
        private readonly IAuditService _audit;
        private readonly IBackupService _backups;
        private readonly ISettingsService _settings;
        private readonly IClock _clock;

        public DashboardService(StudyLinkDbContext db, IBookingService bookings, IRentalService rentals, IFeeService fees,
                                ITimesheetService timesheets, IUserManagementService users, IInventoryService inventory,
                                ITutorVerificationService verification, ISupplierOrderService orders, IAuditService audit,
                                IBackupService backups, ISettingsService settings, IClock clock)
        {
            _db = db;
            _bookings = bookings;
            _rentals = rentals;
            _fees = fees;
            _timesheets = timesheets;
            _users = users;
            _inventory = inventory;
            _verification = verification;
            _orders = orders;
            _audit = audit;
            _backups = backups;
            _settings = settings;
            _clock = clock;
        }

        private string FirstName(Actor actor) => _db.Users.Where(u => u.UserId == actor.UserId).Select(u => u.FirstName).FirstOrDefault();
        private static string R(decimal v) => "R " + v.ToString("N2", Display);

        public StudentDashboard Student(Actor actor)
        {
            var now = _clock.Now;
            var upcoming = _bookings.ForStudent(actor, new BookingFilter { UpcomingOnly = true, PageSize = 5 }).Items;
            var rentals = _rentals.MyRentals(actor).Where(r => r.Status == RentalStatus.Active || r.Status == RentalStatus.PendingPayment).ToList();
            var fees = _fees.Statement(actor);
            var next = upcoming.FirstOrDefault(b => b.Status == BookingStatus.Confirmed && b.SessionDate + b.StartTime > now
                                                    && b.SessionDate + b.StartTime < now.AddHours(48));
            var soonDue = rentals.FirstOrDefault(r => r.Status == RentalStatus.Active && !r.IsOverdue && r.DueDate <= now.Date.AddDays(1));
            return new StudentDashboard
            {
                FirstName = FirstName(actor),
                Reminder = next != null
                    ? "Reminder: your " + next.Subject + " session with " + next.TutorName + " is " + (next.SessionDate.Date == now.Date ? "today" : next.SessionDate.Date == now.Date.AddDays(1) ? "tomorrow" : "on " + next.SessionDate.ToString("dddd", Display)) + " at " + next.StartTime.ToString(@"hh\:mm") + "."
                    : soonDue != null ? "Reminder: \"" + soonDue.BookTitle + "\" is due back " + (soonDue.DueDate.Date == now.Date ? "today." : "tomorrow.") : null,
                Outstanding = fees.Outstanding,
                UnpaidItems = fees.Unpaid.Count,
                Upcoming = upcoming,
                Rentals = rentals,
                ActiveRentals = rentals.Count(r => r.Status == RentalStatus.Active),
                OverdueRentals = rentals.Count(r => r.IsOverdue)
            };
        }

        public TutorDashboard Tutor(Actor actor)
        {
            var tutorId = Profiles.RequireTutor(_db, actor);
            var tutor = _db.Tutors.AsNoTracking().Single(t => t.TutorId == tutorId);
            var today = _clock.Today;
            var weekStart = SchedulePolicy.WeekStart(today);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var week = ReportService.ComputeUtilisation(_db, weekStart, weekStart.AddDays(7)).FirstOrDefault(u => u.TutorId == tutorId);
            var sheet = _timesheets.Week(actor, today);
            var ratings = _db.Reviews.Where(r => r.Booking.TutorId == tutorId && !r.IsHidden).Select(r => (int)r.Rating).ToList();

            return new TutorDashboard
            {
                FirstName = FirstName(actor),
                Status = tutor.VerificationStatus,
                Today = _bookings.ForTutor(actor, new BookingFilter { From = today, To = today, PageSize = 50 }).Items
                    .Where(b => b.Status != BookingStatus.Expired && b.Status != BookingStatus.AwaitingPayment).OrderBy(b => b.StartTime).ToList(),
                Upcoming = _bookings.ForTutor(actor, new BookingFilter { UpcomingOnly = true, PageSize = 8 }).Items
                    .Where(b => b.Status != BookingStatus.AwaitingPayment).ToList(),
                AwaitingResponse = _db.Bookings.Count(b => b.TutorId == tutorId && b.Status == BookingStatus.AwaitingTutor),
                PendingPostponements = _db.PostponementRequests.Count(p => p.TutorId == tutorId && p.Status == PostponementStatus.PendingTutorReview),
                UtilisationPercent = week?.Percent ?? 0,
                EarningsThisMonth = _timesheets.Earnings(actor, monthStart, today).Net,
                AverageRating = ratings.Any() ? Math.Round((decimal)ratings.Average(), 1) : (decimal?)null,
                WeekStart = weekStart,
                TimesheetStatus = sheet.TimesheetId.HasValue ? sheet.Status : (TimesheetStatus?)null,
                TimesheetHours = sheet.TotalHours,
                OpenSlots = _db.TutorAvailability.Count(a => a.TutorSubject.TutorId == tutorId && !a.IsBooked && a.AvailabilityDate >= today)
            }.WithSessionsToday();
        }

        public OperationsDashboard Staff(Actor actor)
        {
            RolePermissions.Demand(actor, Permission.ViewOperationalReports);
            var today = _clock.Today;
            var d = new OperationsDashboard { FirstName = FirstName(actor) };
            var overdue = _rentals.Search(actor, new RentalFilter { OverdueOnly = true, PageSize = 8 });
            d.Overdue = overdue.Items;
            d.PendingTutors = _verification.Queue(actor, TutorVerificationStatus.PendingVerification).Take(6).ToList();
            d.DeliveriesToReceive = _orders.DeliveriesToReceive(actor).Take(6).ToList();

            d.Kpis.Add(new Kpi("Tutors to verify", d.PendingTutors.Count.ToString(Display), "SC22", "~/Operations/TutorVerification.aspx", d.PendingTutors.Count > 0 ? "orange" : "green"));
            d.Kpis.Add(new Kpi("Overdue books", overdue.Total.ToString(Display), "Follow up with students", "~/Operations/Returns.aspx?overdue=1", overdue.Total > 0 ? "red" : "green"));
            d.Kpis.Add(new Kpi("Due back today", _db.Rentals.Count(r => r.Status == RentalStatus.Active && r.DueDate == today).ToString(Display), null, "~/Operations/Returns.aspx", "blue"));
            d.Kpis.Add(new Kpi("Outstanding fines", R(_db.Fines.Where(f => f.Status == FineStatus.Outstanding).Select(f => (decimal?)f.Amount).Sum() ?? 0), null, "~/Operations/Fines.aspx", "orange"));
            if (RolePermissions.Has(actor, Permission.HandlePasswordResets))
                d.Kpis.Add(new Kpi("Password resets", _db.PasswordResetRequests.Count(p => p.Status == RequestStatus.Pending).ToString(Display), "SC27", "~/Operations/PasswordResets.aspx", "navy"));
            d.Kpis.Add(new Kpi("Support messages", _db.SupportMessages.Count(m => m.Status != SupportStatus.Resolved).ToString(Display) + " open",
                _db.ProfileUpdateRequests.Count(p => p.Status == RequestStatus.Pending) + " profile requests", "~/Operations/Support.aspx", "blue"));
            d.Kpis.Add(new Kpi("Timesheets to review", _db.Timesheets.Count(t => t.Status == TimesheetStatus.Submitted).ToString(Display), null, "~/Operations/Timesheets.aspx", "navy"));
            d.Kpis.Add(new Kpi("Deliveries to receive", d.DeliveriesToReceive.Count.ToString(Display), null, "~/Operations/Deliveries.aspx", "green"));
            return d;
        }

        public OperationsDashboard Manager(Actor actor)
        {
            RolePermissions.Demand(actor, Permission.ViewAllReports);
            var now = _clock.Now;
            var today = now.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var d = new OperationsDashboard { FirstName = FirstName(actor) };

            var since = monthStart.AddDays(-31); // computed outside the query: EF6 cannot translate DateTime.AddDays
            var pays = _db.Payments.AsNoTracking().Where(p => p.Status == PaymentStatus.Completed && p.CreatedAt >= since)
                .Select(p => new { p.CreatedAt, p.Type, p.Amount }).ToList();
            Func<DateTime, DateTime, decimal> net = (a, b) => pays.Where(p => p.CreatedAt >= a && p.CreatedAt < b)
                .Sum(p => p.Type == PaymentType.Charge ? p.Amount : -p.Amount);

            var utilisation = ReportService.ComputeUtilisation(_db, monthStart, today.AddDays(1));
            var published = utilisation.Sum(u => u.HoursPublished);
            var lowStock = _inventory.Inventory(actor, new BookFilter { LowStockOnly = true, PageSize = 8 });
            var overdueCount = _db.Rentals.Count(r => r.Status == RentalStatus.Active && r.DueDate < today);

            d.Kpis.Add(new Kpi("Revenue today", R(net(today, today.AddDays(1))), null, "~/Operations/Reports.aspx?key=revenue", "green"));
            d.Kpis.Add(new Kpi("Revenue this month", R(net(monthStart, today.AddDays(1))), today.ToString("MMMM yyyy"), "~/Operations/Reports.aspx?key=revenue", "green"));
            d.Kpis.Add(new Kpi("Outstanding balances", R(_db.Fees.Where(f => f.Status == FeeStatus.Unpaid).Select(f => (decimal?)f.Amount).Sum() ?? 0), null, "~/Operations/Reports.aspx?key=outstanding", "orange"));
            d.Kpis.Add(new Kpi("Books on loan", _db.Rentals.Count(r => r.Status == RentalStatus.Active).ToString(Display), overdueCount + " overdue", "~/Operations/Returns.aspx", overdueCount > 0 ? "red" : "blue"));
            d.Kpis.Add(new Kpi("Books sold this month", _db.Sales.Count(s => s.Status == SaleStatus.Completed && s.CompletedAt >= monthStart).ToString(Display), null, "~/Operations/Reports.aspx?key=sales", "blue"));
            d.Kpis.Add(new Kpi("Tutor utilisation", SchedulePolicy.UtilisationPercent(utilisation.Sum(u => u.HoursBooked), published).ToString("0.0", Display) + "%",
                "this month", "~/Operations/Reports.aspx?key=tutor-utilisation", "navy"));
            d.Kpis.Add(new Kpi("Low-stock titles", lowStock.Total.ToString(Display), null, "~/Manager/Books.aspx?low=1", lowStock.Total > 0 ? "orange" : "green"));
            d.Kpis.Add(new Kpi("Open supplier orders", _db.SupplierOrders.Count(o => o.Status == SupplierOrderStatus.Placed || o.Status == SupplierOrderStatus.Acknowledged || o.Status == SupplierOrderStatus.PartiallyDelivered).ToString(Display),
                _db.SupplierInvoices.Count(i => i.Status == InvoiceStatus.Submitted) + " invoices to review", "~/Manager/SupplierOrders.aspx", "blue"));

            for (var day = today.AddDays(-13); day <= today; day = day.AddDays(1))
                d.RevenueTrend.Add(new TrendPoint { Date = day, Value = net(day, day.AddDays(1)) });
            d.LowStock = lowStock.Items;
            d.Overdue = _rentals.Search(actor, new RentalFilter { OverdueOnly = true, PageSize = 6 }).Items;
            d.PendingTutors = _verification.Queue(actor, TutorVerificationStatus.PendingVerification).Take(5).ToList();
            d.RecentPayments = PaymentService.Rows(_db.Payments.AsNoTracking().Where(p => p.Status == PaymentStatus.Completed)
                .OrderByDescending(p => p.PaymentId).Take(6)).ToList();
            return d;
        }

        public SupplierDashboard Supplier(Actor actor)
        {
            var open = _orders.MyOrders(actor, openOnly: true);
            var invoices = _orders.MyInvoices(actor);
            var titles = _orders.SuppliedTitles(actor);
            var monthStart = new DateTime(_clock.Today.Year, _clock.Today.Month, 1);
            var d = new SupplierDashboard
            {
                CompanyName = _db.Suppliers.Where(s => s.UserId == actor.UserId).Select(s => s.CompanyName).FirstOrDefault(),
                OpenOrders = open,
                RecentInvoices = invoices.Take(6).ToList(),
                LowStockTitles = titles.Where(t => t.IsLow).Take(8).ToList()
            };
            d.Kpis.Add(new Kpi("Open orders", open.Count.ToString(Display), open.Count(o => o.Status == SupplierOrderStatus.Placed) + " to acknowledge", "~/Supplier/Orders.aspx", "blue"));
            d.Kpis.Add(new Kpi("Units to deliver", open.Sum(o => o.UnitsOrdered - o.UnitsDelivered).ToString(Display), null, "~/Supplier/Orders.aspx", "orange"));
            d.Kpis.Add(new Kpi("Deliveries in transit", open.Sum(o => o.PendingDeliveries).ToString(Display), "awaiting hub receipt", "~/Supplier/Deliveries.aspx", "navy"));
            d.Kpis.Add(new Kpi("Invoices awaiting payment", R(invoices.Where(i => i.Status == InvoiceStatus.Submitted || i.Status == InvoiceStatus.Approved).Sum(i => i.Amount)),
                null, "~/Supplier/Invoices.aspx", "orange"));
            d.Kpis.Add(new Kpi("Paid this month", R(invoices.Where(i => i.Status == InvoiceStatus.Paid && i.PaidAt >= monthStart).Sum(i => i.Amount)), null, "~/Supplier/Invoices.aspx", "green"));
            return d;
        }

        public AdminDashboard Admin(Actor actor)
        {
            RolePermissions.Demand(actor, Permission.ManageAllUsers);
            var today = _clock.Today;
            var d = new AdminDashboard
            {
                FirstName = FirstName(actor),
                RecentAudit = _audit.Search(actor, new AuditFilter { PageSize = 8 }).Items,
                Roles = _users.Roles(actor),
                LockedAccounts = _users.LockedAccounts(actor),
                LastBackup = _backups.History(actor).FirstOrDefault(),
                DatabaseName = _db.Database.Connection.Database
            };
            try
            {
                var mb = _db.Database.SqlQuery<decimal>("SELECT CAST(SUM(CAST(size AS bigint)) * 8.0 / 1024 AS decimal(12,1)) FROM sys.database_files").FirstOrDefault();
                d.DatabaseSize = mb.ToString("N1", Display) + " MB";
            }
            catch (Exception)
            {
                d.DatabaseSize = "unavailable";
            }

            var users = _db.Users.Count();
            var active = _db.Users.Count(u => u.IsActive);
            d.Kpis.Add(new Kpi("User accounts", users.ToString(Display), active + " active", "~/Operations/Users.aspx", "blue"));
            d.Kpis.Add(new Kpi("Locked accounts", d.LockedAccounts.Count.ToString(Display), null, "~/Admin/Security.aspx", d.LockedAccounts.Count > 0 ? "red" : "green"));
            d.Kpis.Add(new Kpi("Password resets", _db.PasswordResetRequests.Count(p => p.Status == RequestStatus.Pending).ToString(Display), "pending", "~/Operations/PasswordResets.aspx", "orange"));
            d.Kpis.Add(new Kpi("Audit events today", _db.AuditLogs.Count(a => a.CreatedAt >= today).ToString(Display), null, "~/Admin/AuditLog.aspx", "navy"));
            d.Kpis.Add(new Kpi("Last backup", d.LastBackup == null ? "Never" : d.LastBackup.StartedAt.ToString("d MMM HH:mm", Display),
                d.LastBackup?.Status.ToString(), "~/Admin/Backups.aspx", d.LastBackup?.Status == BackupStatus.Succeeded ? "green" : "orange"));
            d.Kpis.Add(new Kpi("Database size", d.DatabaseSize, d.DatabaseName, "~/Admin/Backups.aspx", "navy"));
            return d;
        }
    }

    internal static class DashboardExtensions
    {
        public static TutorDashboard WithSessionsToday(this TutorDashboard d)
        {
            d.SessionsToday = d.Today.Count(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.AwaitingTutor || b.Status == BookingStatus.Completed);
            return d;
        }
    }

    /// <summary>
    /// Hourly housekeeping (web BackgroundSweep). Every step is idempotent: holds are expired by
    /// state, reminders carry a DedupKey, metrics are upserted by date.
    /// </summary>
    public sealed class MaintenanceService : IMaintenanceService
    {
        private readonly StudyLinkDbContext _db;
        private readonly IBookingService _bookings;
        private readonly IRentalService _rentals;
        private readonly INotificationService _notifications;
        private readonly IClock _clock;

        public MaintenanceService(StudyLinkDbContext db, IBookingService bookings, IRentalService rentals,
                                  INotificationService notifications, IClock clock)
        {
            _db = db;
            _bookings = bookings;
            _rentals = rentals;
            _notifications = notifications;
            _clock = clock;
        }

        public SweepSummary Run()
        {
            var now = _clock.Now;
            var today = now.Date;
            var summary = new SweepSummary { RanAt = now };

            summary.BookingsExpired = _bookings.ExpireStale();
            summary.HoldsReleased = _rentals.ExpireHolds();

            // Due-tomorrow reminders (once per rental per due date).
            var tomorrow = today.AddDays(1);
            foreach (var r in _db.Rentals.Include(x => x.Student).Include(x => x.Copy.Book)
                         .Where(x => x.Status == RentalStatus.Active && x.DueDate == tomorrow).ToList())
            {
                _notifications.Notify(r.Student.UserId, NotificationType.RentalDueSoon, "Book due tomorrow",
                    "\"" + r.Copy.Book.Title + "\" is due back tomorrow. Renew it in My Rentals or return it to the hub.",
                    "~/Student/Books/MyRentals.aspx", "RentalDue:" + r.RentalId + ":" + r.DueDate.ToString("yyyy-MM-dd"));
                summary.RemindersSent++;
            }

            // Overdue notices: on the first day overdue and then weekly.
            foreach (var r in _db.Rentals.Include(x => x.Student).Include(x => x.Copy.Book)
                         .Where(x => x.Status == RentalStatus.Active && x.DueDate < today).ToList())
            {
                var days = RentalPolicy.DaysLate(r.DueDate, today);
                if (days != 1 && days % 7 != 0) continue;
                _notifications.Notify(r.Student.UserId, NotificationType.RentalOverdue, "Book overdue",
                    "\"" + r.Copy.Book.Title + "\" is " + days + " day(s) overdue. A late fee of R " + r.Copy.Book.LateFeePerDay.ToString("N2") + " per day applies.",
                    "~/Student/Books/MyRentals.aspx", "RentalOverdue:" + r.RentalId + ":" + today.ToString("yyyy-MM-dd"));
                summary.OverdueNotices++;
            }

            // Session reminders for tomorrow.
            foreach (var b in _db.Bookings.Include(x => x.Student).Include(x => x.TutorSubject.Subject)
                         .Where(x => x.Status == BookingStatus.Confirmed && x.SessionDate == tomorrow).ToList())
                _notifications.Notify(b.Student.UserId, NotificationType.BookingConfirmed, "Session tomorrow",
                    b.TutorSubject.Subject.Name + " tomorrow at " + b.StartTime.ToString(@"hh\:mm") + ".",
                    "~/Student/Tutoring/BookingDetails.aspx?id=" + b.BookingId, "SessionReminder:" + b.BookingId + ":" + b.SessionDate.ToString("yyyy-MM-dd"));
            _db.SaveChanges();

            summary.LowStockAlerts = LowStockAlerts(today);
            CaptureMetrics(today.AddDays(-1));
            CaptureMetrics(today);
            summary.MetricsCaptured = true;
            summary.TutorStatsRefreshed = RefreshTutorStats(today);
            return summary;
        }

        private int LowStockAlerts(DateTime today)
        {
            var low = _db.Books.AsNoTracking().Where(b => b.IsActive && b.ReorderLevel > 0)
                .Select(b => new { b.BookId, b.Title, b.ReorderLevel, Available = b.Copies.Count(c => c.Status == CopyStatus.Available) })
                .ToList().Where(b => b.Available < b.ReorderLevel).ToList();
            if (low.Count == 0) return 0;
            var managers = _db.UserRoles.Where(ur => ur.Role.Name == RoleNames.Manager && ur.User.IsActive).Select(ur => ur.UserId).Distinct().ToList();
            var sent = 0;
            foreach (var b in low)
                foreach (var m in managers)
                {
                    _notifications.Notify(m, NotificationType.LowStock, "Low stock: " + b.Title,
                        b.Available + " available (reorder level " + b.ReorderLevel + "). Consider a supplier order.",
                        "~/Manager/BookEdit.aspx?id=" + b.BookId, "LowStock:" + b.BookId + ":" + m + ":" + today.ToString("yyyy-MM-dd"));
                    sent++;
                }
            _db.SaveChanges();
            return low.Count;
        }

        /// <summary>Upserts one day's snapshot from the transaction tables.</summary>
        private void CaptureMetrics(DateTime day)
        {
            var next = day.AddDays(1);
            var pays = _db.Payments.Where(p => p.Status == PaymentStatus.Completed && p.CreatedAt >= day && p.CreatedAt < next)
                .Select(p => new { p.Type, p.Amount }).ToList();
            var studentRole = RoleNames.Student;
            var metric = _db.DailyMetrics.Find(day) ?? _db.DailyMetrics.Add(new DailyMetric { MetricDate = day });
            metric.Revenue = pays.Where(p => p.Type == PaymentType.Charge).Sum(p => p.Amount);
            metric.Refunds = pays.Where(p => p.Type == PaymentType.Refund).Sum(p => p.Amount);
            metric.PaymentsCount = pays.Count(p => p.Type == PaymentType.Charge);
            metric.BookingsCreated = _db.Bookings.Count(b => b.CreatedAt >= day && b.CreatedAt < next && b.Status != BookingStatus.Expired);
            metric.SessionsCompleted = _db.TutoringSessions.Count(s => s.SessionDate == day);
            metric.RentalsStarted = _db.Rentals.Count(r => r.RentedAt >= day && r.RentedAt < next && (r.Status == RentalStatus.Active || r.Status == RentalStatus.Returned || r.Status == RentalStatus.Lost));
            metric.RentalsReturned = _db.Rentals.Count(r => r.ReturnedAt >= day && r.ReturnedAt < next);
            metric.OverdueRentals = _db.Rentals.Count(r => r.Status == RentalStatus.Active && r.DueDate < day);
            metric.BooksSold = _db.Sales.Count(s => s.Status == SaleStatus.Completed && s.CompletedAt >= day && s.CompletedAt < next);
            metric.FinesIssued = _db.Fines.Where(f => f.CreatedAt >= day && f.CreatedAt < next).Select(f => (decimal?)f.Amount).Sum() ?? 0;
            metric.NewStudents = _db.UserRoles.Count(ur => ur.Role.Name == studentRole && ur.AssignedAt >= day && ur.AssignedAt < next);
            metric.NewTutors = _db.Tutors.Count(t => t.CreatedAt >= day && t.CreatedAt < next);
            metric.CapturedAt = _clock.Now;
            _db.SaveChanges();
        }

        private int RefreshTutorStats(DateTime today)
        {
            var from = today.AddDays(-30);
            var utilisation = ReportService.ComputeUtilisation(_db, from, today.AddDays(1));
            var ratings = _db.Reviews.Where(r => !r.IsHidden).GroupBy(r => r.Booking.TutorId)
                .Select(g => new { TutorId = g.Key, Avg = g.Average(r => (decimal)r.Rating), N = g.Count() }).ToList();
            var totals = _db.TutoringSessions.GroupBy(s => s.Booking.TutorId)
                .Select(g => new { TutorId = g.Key, Hours = g.Sum(s => s.HoursDelivered), Gross = g.Sum(s => s.Booking.Fee), N = g.Count() }).ToList();
            var cancelled = _db.Bookings.Where(b => b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.Declined)
                .GroupBy(b => b.TutorId).Select(g => new { TutorId = g.Key, N = g.Count() }).ToList();

            foreach (var u in utilisation)
            {
                var stat = _db.TutorStats.Find(u.TutorId) ?? _db.TutorStats.Add(new TutorStat { TutorId = u.TutorId });
                var rating = ratings.FirstOrDefault(r => r.TutorId == u.TutorId);
                var total = totals.FirstOrDefault(t => t.TutorId == u.TutorId);
                stat.SessionsCompleted = total?.N ?? 0;
                stat.HoursDelivered = total?.Hours ?? 0;
                stat.GrossEarnings = total?.Gross ?? 0;
                stat.SessionsCancelled = cancelled.FirstOrDefault(c => c.TutorId == u.TutorId)?.N ?? 0;
                stat.HoursPublished = u.HoursPublished;
                stat.UtilisationPercent = u.Percent;
                stat.AverageRating = rating == null ? (decimal?)null : Math.Round(rating.Avg, 2);
                stat.ReviewCount = rating?.N ?? 0;
                stat.RefreshedAt = _clock.Now;
            }
            _db.SaveChanges();
            return utilisation.Count;
        }
    }
}
