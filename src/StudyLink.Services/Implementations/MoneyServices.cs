using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
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

namespace StudyLink.Services.Implementations
{
    /// <summary>Creates and cancels ledger rows. Every billable event goes through here so My Fees is complete.</summary>
    internal static class FeeLedger
    {
        public static Fee Add(StudyLinkDbContext db, int studentId, FeeType type, decimal amount, string description, DateTime now,
                              Booking booking = null, Rental rental = null, RentalRenewal renewal = null, Sale sale = null, Fine fine = null)
        {
            return db.Fees.Add(new Fee
            {
                StudentId = studentId, Type = type, Amount = MoneyPolicy.Round(amount),
                Description = description.Length > 200 ? description.Substring(0, 200) : description,
                Status = FeeStatus.Unpaid, CreatedAt = now,
                Booking = booking, Rental = rental, RentalRenewal = renewal, Sale = sale, Fine = fine
            });
        }

        public static void CancelUnpaid(StudyLinkDbContext db, int? bookingId = null, int? rentalId = null, int? saleId = null)
        {
            var fees = db.Fees.Where(f => f.Status == FeeStatus.Unpaid
                                          && ((bookingId.HasValue && f.BookingId == bookingId)
                                              || (rentalId.HasValue && f.RentalId == rentalId)
                                              || (saleId.HasValue && f.SaleId == saleId))).ToList();
            foreach (var f in fees)
                f.Status = FeeStatus.Cancelled;
        }

        public static IQueryable<FeeRow> Rows(IQueryable<Fee> q) =>
            q.Select(f => new FeeRow
            {
                FeeId = f.FeeId, Type = f.Type, Description = f.Description, Amount = f.Amount, Status = f.Status,
                CreatedAt = f.CreatedAt, SettledAt = f.SettledAt,
                SourceReference = f.Booking != null ? f.Booking.Reference
                    : f.Rental != null ? f.Rental.Reference
                    : f.RentalRenewal != null ? f.RentalRenewal.Rental.Reference
                    : f.Sale != null ? f.Sale.Reference
                    : f.Fine != null ? f.Fine.Rental.Reference : null,
                PayBy = f.Booking != null ? f.Booking.HoldExpiresAt
                    : f.Rental != null ? f.Rental.HoldExpiresAt
                    : f.Sale != null ? f.Sale.HoldExpiresAt : null
            });
    }

    public sealed class FeeService : IFeeService
    {
        private readonly StudyLinkDbContext _db;
        private readonly IClock _clock;

        public FeeService(StudyLinkDbContext db, IClock clock)
        {
            _db = db;
            _clock = clock;
        }

        public FeeStatement Statement(Actor actor)
        {
            RolePermissions.Demand(actor, Permission.PayFees);
            return Build(Profiles.RequireStudent(_db, actor));
        }

        public FeeStatement StatementFor(Actor actor, int studentId)
        {
            RolePermissions.Demand(actor, Permission.ManageFines);
            return Build(studentId);
        }

        private FeeStatement Build(int studentId)
        {
            var fees = _db.Fees.AsNoTracking().Where(f => f.StudentId == studentId);
            var unpaid = FeeLedger.Rows(fees.Where(f => f.Status == FeeStatus.Unpaid).OrderBy(f => f.CreatedAt)).ToList();
            var yearStart = new DateTime(_clock.Today.Year, 1, 1);
            return new FeeStatement
            {
                Unpaid = unpaid,
                Outstanding = unpaid.Sum(f => f.Amount),
                History = FeeLedger.Rows(fees.Where(f => f.Status != FeeStatus.Unpaid).OrderByDescending(f => f.FeeId).Take(50)).ToList(),
                PaidThisYear = _db.Payments.Where(p => p.StudentId == studentId && p.Type == PaymentType.Charge
                                                       && p.Status == PaymentStatus.Completed && p.CreatedAt >= yearStart)
                                   .Select(p => (decimal?)p.Amount).Sum() ?? 0,
                Payments = PaymentService.Rows(_db.Payments.AsNoTracking().Where(p => p.StudentId == studentId && p.Status != PaymentStatus.Failed)
                    .OrderByDescending(p => p.PaymentId).Take(20)).ToList()
            };
        }

        public ServiceResult<CheckoutSummary> PrepareCheckout(Actor actor, IList<int> feeIds)
        {
            RolePermissions.Demand(actor, Permission.PayFees);
            var studentId = Profiles.RequireStudent(_db, actor);
            return Validate(_db, studentId, feeIds, _clock.Now);
        }

        /// <summary>Shared by checkout display and the payment itself (which re-runs it under lock).</summary>
        internal static ServiceResult<CheckoutSummary> Validate(StudyLinkDbContext db, int studentId, IList<int> feeIds, DateTime now)
        {
            var ids = (feeIds ?? new List<int>()).Distinct().ToList();
            if (ids.Count == 0)
                return ServiceResult<CheckoutSummary>.Fail("Select at least one fee to pay.");

            var fees = db.Fees.Include(f => f.Booking).Include(f => f.Rental).Include(f => f.Sale)
                .Where(f => ids.Contains(f.FeeId) && f.StudentId == studentId).ToList();
            if (fees.Count != ids.Count)
                return ServiceResult<CheckoutSummary>.Fail("One of the selected fees was not found.");
            if (fees.Any(f => f.Status != FeeStatus.Unpaid))
                return ServiceResult<CheckoutSummary>.Fail("One of the selected fees has already been paid or cancelled. Refresh My Fees.");

            foreach (var f in fees)
            {
                if (f.Booking != null && (f.Booking.Status != BookingStatus.AwaitingPayment || f.Booking.HoldExpiresAt < now))
                    return ServiceResult<CheckoutSummary>.Fail("The reservation " + f.Booking.Reference + " has expired. Please book the slot again.");
                if (f.Rental != null && (f.Rental.Status != RentalStatus.PendingPayment || f.Rental.HoldExpiresAt < now))
                    return ServiceResult<CheckoutSummary>.Fail("The rental reservation " + f.Rental.Reference + " has expired. Please reserve the book again.");
                if (f.Sale != null && (f.Sale.Status != SaleStatus.PendingPayment || f.Sale.HoldExpiresAt < now))
                    return ServiceResult<CheckoutSummary>.Fail("The purchase " + f.Sale.Reference + " has expired. Please add the book again.");
            }

            var rows = FeeLedger.Rows(db.Fees.Where(f => ids.Contains(f.FeeId))).ToList();
            var holds = rows.Where(r => r.PayBy.HasValue).Select(r => r.PayBy.Value).ToList();
            return ServiceResult<CheckoutSummary>.Ok(new CheckoutSummary
            {
                Fees = rows,
                Total = MoneyPolicy.Round(rows.Sum(r => r.Amount)),
                HoldExpiresAt = holds.Any() ? holds.Min() : (DateTime?)null,
                Purpose = rows.Count == 1 ? rows[0].Description : rows.Count + " items"
            });
        }
    }

    public sealed class PaymentService : IPaymentService
    {
        private readonly StudyLinkDbContext _db;
        private readonly IPaymentGateway _gateway;
        private readonly ISettingsService _settings;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IEmailSender _email;
        private readonly IClock _clock;

        public PaymentService(StudyLinkDbContext db, IPaymentGateway gateway, ISettingsService settings, IAuditService audit,
                              INotificationService notifications, IEmailSender email, IClock clock)
        {
            _db = db;
            _gateway = gateway;
            _settings = settings;
            _audit = audit;
            _notifications = notifications;
            _email = email;
            _clock = clock;
        }

        public ServiceResult<int> Pay(Actor actor, IList<int> feeIds, CardDetails card, Guid checkoutToken)
        {
            RolePermissions.Demand(actor, Permission.PayFees);
            var studentId = Profiles.RequireStudent(_db, actor);

            // Idempotency: the same checkout submitted twice returns the first successful payment.
            var already = _db.Payments.Where(p => p.CheckoutToken == checkoutToken && p.StudentId == studentId)
                .Select(p => (int?)p.PaymentId).FirstOrDefault();
            if (already.HasValue)
                return ServiceResult<int>.Ok(already.Value);

            var ids = (feeIds ?? new List<int>()).Distinct().ToList();
            var now = _clock.Now;
            Payment payment;
            try
            {
                using (var tx = _db.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    // Lock the fee rows so the background sweep cannot expire them mid-payment.
                    if (ids.Count > 0)
                    {
                        var inList = string.Join(",", ids.Select((_, i) => "@p" + i));
                        _db.Database.SqlQuery<int>("SELECT FeeId FROM dbo.Fees WITH (UPDLOCK, HOLDLOCK) WHERE FeeId IN (" + inList + ")",
                            ids.Cast<object>().ToArray()).ToList();
                    }

                    var check = FeeService.Validate(_db, studentId, ids, now);
                    if (!check.Succeeded)
                        return ServiceResult<int>.Fail(check.Error);
                    var summary = check.Value;
                    if (summary.Total <= 0)
                        return ServiceResult<int>.Fail("There is nothing to pay.");

                    var result = _gateway.Charge(summary.Total, card, "StudyLink: " + summary.Purpose);
                    if (!result.Approved)
                    {
                        // Keep a record of the failed attempt (without the token, so the student can retry).
                        _db.Payments.Add(new Payment
                        {
                            Reference = ReferenceGenerator.Payment(now), StudentId = studentId, Type = PaymentType.Charge,
                            Status = PaymentStatus.Failed, Amount = summary.Total, CardBrand = result.Brand, CardLast4 = result.Last4,
                            FailureReason = AuditService.Truncate(result.DeclineReason, 200), CreatedAt = now, ProcessedByUserId = actor.UserId
                        });
                        _db.SaveChanges();
                        tx.Commit();
                        return ServiceResult<int>.Fail(result.DeclineReason ?? "The payment was declined.");
                    }

                    payment = _db.Payments.Add(new Payment
                    {
                        Reference = ReferenceGenerator.Payment(now), StudentId = studentId, Type = PaymentType.Charge,
                        Status = PaymentStatus.Completed, Amount = summary.Total, CardBrand = result.Brand, CardLast4 = result.Last4,
                        GatewayReference = result.Reference, CheckoutToken = checkoutToken, CreatedAt = now, CompletedAt = now,
                        ProcessedByUserId = actor.UserId
                    });

                    var fees = _db.Fees.Where(f => ids.Contains(f.FeeId)).ToList();
                    var allocationError = MoneyPolicy.CheckAllocation(payment.Amount, fees.Select(f => f.Amount));
                    if (allocationError != null)
                        throw new InvalidOperationException(allocationError); // cannot happen: total was computed from these fees

                    foreach (var fee in fees)
                    {
                        _db.PaymentAllocations.Add(new PaymentAllocation { Payment = payment, FeeId = fee.FeeId, Amount = fee.Amount });
                        fee.Status = FeeStatus.Paid;
                        fee.SettledAt = now;
                        Settle(fee, now);
                    }

                    _audit.Record(actor, "PaymentRecorded", "Payment", payment.Reference,
                        newState: payment.Amount.ToString("0.00", CultureInfo.InvariantCulture), details: summary.Purpose);
                    _db.SaveChanges();
                    tx.Commit();
                }
            }
            catch (Exception ex) when (DbErrors.IsUniqueViolation(ex))
            {
                var first = _db.Payments.AsNoTracking().Where(p => p.CheckoutToken == checkoutToken).Select(p => (int?)p.PaymentId).FirstOrDefault();
                return first.HasValue ? ServiceResult<int>.Ok(first.Value) : ServiceResult<int>.Fail("The payment could not be completed. Please try again.");
            }
            catch (Exception ex) when (DbErrors.IsDeadlock(ex))
            {
                return ServiceResult<int>.Fail("The payment could not be completed because the items changed. Please review My Fees and try again.");
            }

            // Sent after commit because the receipt link needs the new payment's id.
            _notifications.Notify(actor.UserId, NotificationType.PaymentRecorded, "Payment received",
                "We received R " + payment.Amount.ToString("N2", CultureInfo.GetCultureInfo("en-GB")) + " (" + payment.Reference + ").",
                "~/Student/Fees/PaymentComplete.aspx?id=" + payment.PaymentId);
            _db.SaveChanges();
            SendReceipt(payment.PaymentId);
            return ServiceResult<int>.Ok(payment.PaymentId);
        }

        /// <summary>What paying a fee unlocks. Runs inside the payment transaction.</summary>
        private void Settle(Fee fee, DateTime now)
        {
            if (fee.BookingId.HasValue)
            {
                var b = _db.Bookings.Include(x => x.Tutor).Include(x => x.TutorSubject.Subject).Single(x => x.BookingId == fee.BookingId);
                b.Status = BookingStatus.AwaitingTutor;
                b.PaidAt = now;
                b.HoldExpiresAt = null;
                _notifications.Notify(b.Tutor.UserId, NotificationType.BookingCreated, "New session to confirm",
                    b.TutorSubject.Subject.Name + " on " + Profiles.When(b.SessionDate, b.StartTime, b.EndTime) + ". Please confirm or decline.",
                    "~/Tutor/SessionDetails.aspx?id=" + b.BookingId);
            }
            else if (fee.RentalId.HasValue)
            {
                var r = _db.Rentals.Include(x => x.Copy.Book).Single(x => x.RentalId == fee.RentalId);
                r.Status = RentalStatus.Active;
                r.RentedAt = now;
                r.DueDate = RentalPolicy.DueDate(now, _settings.GetInt(SettingKeys.RentalPeriodDays, 7));
                r.HoldExpiresAt = null;
                r.Copy.Status = CopyStatus.Rented;
                _notifications.Notify(_db.Students.Where(s => s.StudentId == r.StudentId).Select(s => s.UserId).Single(),
                    NotificationType.RentalConfirmed, "Rental confirmed",
                    "Collect \"" + r.Copy.Book.Title + "\" at the hub. It is due back on " + r.DueDate.ToString("d MMM yyyy") + ".",
                    "~/Student/Books/MyRentals.aspx");
            }
            else if (fee.SaleId.HasValue)
            {
                var s = _db.Sales.Include(x => x.Copy).Single(x => x.SaleId == fee.SaleId);
                s.Status = SaleStatus.Completed;
                s.CompletedAt = now;
                s.HoldExpiresAt = null;
                s.Copy.Status = CopyStatus.Sold;
            }
            else if (fee.FineId.HasValue)
            {
                _db.Fines.Find(fee.FineId.Value).Status = FineStatus.Paid;
            }
        }

        private void SendReceipt(int paymentId)
        {
            var r = Build(paymentId);
            var rows = string.Join("", r.Lines.Select(l => "<tr><td>" + Accounts.Html(l.Description) + "</td><td style=\"text-align:right\">R "
                                                          + l.Amount.ToString("N2", CultureInfo.GetCultureInfo("en-GB")) + "</td></tr>"));
            _email.Send(r.StudentEmail, "StudyLink receipt " + r.Reference,
                "<p>Hello " + Accounts.Html(r.StudentName) + ",</p><p>Thank you for your payment to " + Accounts.Html(r.Organisation) + ".</p>"
                + "<table>" + rows + "<tr><th>Total</th><th style=\"text-align:right\">R "
                + r.Amount.ToString("N2", CultureInfo.GetCultureInfo("en-GB")) + "</th></tr></table>"
                + "<p>Reference: <b>" + r.Reference + "</b><br>Card: " + Accounts.Html(r.CardBrand) + " ending " + r.CardLast4
                + "<br>Date: " + r.CreatedAt.ToString("d MMM yyyy HH:mm") + "</p>");
        }

        public Receipt Receipt(Actor actor, int paymentId)
        {
            var owner = _db.Payments.Where(p => p.PaymentId == paymentId).Select(p => (int?)p.Student.UserId).FirstOrDefault();
            if (owner == null) return null;
            if (owner != actor.UserId && !RolePermissions.Has(actor, Permission.ManagePayments) && !RolePermissions.Has(actor, Permission.ManageFines))
                return null;
            return Build(paymentId);
        }

        private Receipt Build(int paymentId)
        {
            var p = _db.Payments.AsNoTracking().Include(x => x.Student.User).Single(x => x.PaymentId == paymentId);
            var lines = _db.PaymentAllocations.AsNoTracking().Where(a => a.PaymentId == paymentId || (p.RefundOfPaymentId != null && a.PaymentId == p.RefundOfPaymentId))
                .Select(a => new { a.Fee.Description, a.Fee.Type, a.Amount, a.Fee.BookingId, a.Fee.RentalId, a.Fee.SaleId }).ToList();
            var studentId = p.StudentId;
            return new Receipt
            {
                PaymentId = p.PaymentId, Reference = p.Reference, Type = p.Type, Status = p.Status, Amount = p.Amount,
                CardBrand = p.CardBrand, CardLast4 = p.CardLast4, GatewayReference = p.GatewayReference, CreatedAt = p.CreatedAt,
                StudentName = p.Student.User.FullName, StudentNumber = p.Student.StudentNumber, StudentEmail = p.Student.User.Email,
                Organisation = _settings.GetString(SettingKeys.OrganisationName, "Forte Books & Tuition Hub"),
                OutstandingAfter = _db.Fees.Where(f => f.StudentId == studentId && f.Status == FeeStatus.Unpaid).Select(f => (decimal?)f.Amount).Sum() ?? 0,
                Lines = lines.Select(l => new ReceiptLine { Description = l.Description, Type = l.Type, Amount = l.Amount }).ToList(),
                BookingId = lines.Select(l => l.BookingId).FirstOrDefault(x => x.HasValue),
                RentalId = lines.Select(l => l.RentalId).FirstOrDefault(x => x.HasValue),
                SaleId = lines.Select(l => l.SaleId).FirstOrDefault(x => x.HasValue)
            };
        }

        public PagedResult<PaymentRow> Search(Actor actor, PaymentFilter f)
        {
            RolePermissions.Demand(actor, Permission.ManagePayments);
            var q = _db.Payments.AsNoTracking().AsQueryable();
            if (f.From.HasValue) { var from = f.From.Value.Date; q = q.Where(p => p.CreatedAt >= from); }
            if (f.To.HasValue) { var to = f.To.Value.Date.AddDays(1); q = q.Where(p => p.CreatedAt < to); }
            if (f.Type.HasValue) q = q.Where(p => p.Type == f.Type.Value);
            if (f.Status.HasValue) q = q.Where(p => p.Status == f.Status.Value);
            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var s = f.Search.Trim();
                q = q.Where(p => p.Reference.Contains(s) || p.Student.StudentNumber.Contains(s) || p.Student.User.LastName.Contains(s));
            }
            var total = q.Count();
            var page = Math.Max(1, f.Page);
            var rows = Rows(q.OrderByDescending(p => p.PaymentId).Skip((page - 1) * f.PageSize).Take(f.PageSize)).ToList();
            return new PagedResult<PaymentRow>(rows, total, page, f.PageSize);
        }

        internal static IQueryable<PaymentRow> Rows(IQueryable<Payment> q) =>
            q.Select(p => new PaymentRow
            {
                PaymentId = p.PaymentId, Reference = p.Reference,
                StudentName = p.Student.User.FirstName + " " + p.Student.User.LastName, StudentNumber = p.Student.StudentNumber,
                Type = p.Type, Status = p.Status, Amount = p.Amount,
                Card = p.CardBrand == null ? null : p.CardBrand + " •••• " + p.CardLast4,
                Items = p.Allocations.Count() == 1 ? p.Allocations.Select(a => a.Fee.Description).FirstOrDefault()
                      : p.Allocations.Count() > 1 ? p.Allocations.Count() + " items"
                      : p.Note,
                CreatedAt = p.CreatedAt, FailureReason = p.FailureReason
            });

        public ServiceResult Refund(Actor actor, int feeId, string reason)
        {
            RolePermissions.Demand(actor, Permission.ManagePayments);
            if (string.IsNullOrWhiteSpace(reason)) return ServiceResult.Fail("Give a reason for the refund.");
            return RefundCore(actor, feeId, reason.Trim());
        }

        public ServiceResult RefundForRule(Actor actor, int feeId, string reason) => RefundCore(actor, feeId, reason);

        private ServiceResult RefundCore(Actor actor, int feeId, string reason)
        {
            var fee = _db.Fees.Include(f => f.Student).FirstOrDefault(f => f.FeeId == feeId);
            if (fee == null) return ServiceResult.Fail("Fee not found.");
            if (fee.Status != FeeStatus.Paid) return ServiceResult.Fail("Only a paid fee can be refunded.");

            var now = _clock.Now;
            var allocations = _db.PaymentAllocations.Include(a => a.Payment)
                .Where(a => a.FeeId == feeId && a.Payment.Type == PaymentType.Charge && a.Payment.Status == PaymentStatus.Completed).ToList();
            foreach (var a in allocations)
            {
                var result = _gateway.Refund(a.Payment.GatewayReference, a.Amount);
                if (!result.Approved) return ServiceResult.Fail("The card refund was not accepted by the payment provider.");
                _db.Payments.Add(new Payment
                {
                    Reference = ReferenceGenerator.Payment(now).Replace("SL-PAY", "SL-REF"), StudentId = fee.StudentId,
                    Type = PaymentType.Refund, Status = PaymentStatus.Completed, Amount = a.Amount,
                    CardBrand = a.Payment.CardBrand, CardLast4 = a.Payment.CardLast4, GatewayReference = result.Reference,
                    RefundOfPaymentId = a.PaymentId, Note = AuditService.Truncate("Refund: " + fee.Description, 200),
                    CreatedAt = now, CompletedAt = now, ProcessedByUserId = actor.IsSystem ? (int?)null : actor.UserId
                });
            }
            fee.Status = FeeStatus.Refunded;
            fee.SettledAt = now;
            _audit.Record(actor, "PaymentRefunded", "Fee", feeId, "Paid", "Refunded", AuditService.Truncate(reason, 500));
            _notifications.Notify(fee.Student.UserId, NotificationType.RefundIssued, "Refund issued",
                "R " + fee.Amount.ToString("N2", CultureInfo.GetCultureInfo("en-GB")) + " was refunded to your card: " + fee.Description,
                "~/Student/Fees/MyFees.aspx");
            _db.SaveChanges();
            return ServiceResult.Ok();
        }
    }
}
