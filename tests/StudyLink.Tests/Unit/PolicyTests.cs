using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StudyLink.Data.Entities;
using StudyLink.Data.Seed;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Policies;
using StudyLink.Services.Security;
using StudyLink.Services.Validation;

namespace StudyLink.Tests.Unit
{
    [TestClass]
    [TestCategory("Unit")]
    public class SchedulePolicyTests
    {
        private static TimeSpan H(double h) => TimeSpan.FromHours(h);
        private static readonly DateTime Now = new DateTime(2026, 10, 5, 9, 0, 0);

        [DataTestMethod]
        [DataRow(10, 11, 11, 12, false, DisplayName = "back-to-back does not clash (half-open)")]
        [DataRow(10, 12, 11, 13, true, DisplayName = "partial overlap")]
        [DataRow(10, 14, 11, 12, true, DisplayName = "containment")]
        [DataRow(9, 10, 11, 12, false, DisplayName = "separate")]
        public void Overlaps(double aStart, double aEnd, double bStart, double bEnd, bool expected)
        {
            Assert.AreEqual(expected, SchedulePolicy.Overlaps(H(aStart), H(aEnd), H(bStart), H(bEnd)));
            Assert.AreEqual(expected, SchedulePolicy.Overlaps(H(bStart), H(bEnd), H(aStart), H(aEnd)), "symmetric");
        }

        [TestMethod]
        public void DifferentDaysNeverOverlap() =>
            Assert.IsFalse(SchedulePolicy.Overlaps(Now.Date, H(10), H(12), Now.Date.AddDays(1), H(10), H(12)));

        [TestMethod]
        public void SlotValidation()
        {
            var tomorrow = Now.Date.AddDays(1);
            Assert.IsNull(SchedulePolicy.CheckSlot(tomorrow, H(10), H(11), Now, 8, 365));
            Assert.IsNotNull(SchedulePolicy.CheckSlot(tomorrow, H(11), H(10), Now, 8, 365), "end before start (BR-06)");
            Assert.IsNotNull(SchedulePolicy.CheckSlot(tomorrow, H(10), H(10.25), Now, 8, 365), "under 30 minutes");
            Assert.IsNotNull(SchedulePolicy.CheckSlot(tomorrow, H(8), H(17), Now, 8, 365), "longer than 8 hours");
            Assert.IsNotNull(SchedulePolicy.CheckSlot(Now.Date, H(8), H(10), Now, 8, 365), "started in the past (BR-07)");
            Assert.IsNotNull(SchedulePolicy.CheckSlot(Now.Date.AddDays(400), H(8), H(10), Now, 8, 365), "too far ahead");
        }

        [TestMethod]
        public void SessionFeeIsRateTimesLength()
        {
            Assert.AreEqual(80m, SchedulePolicy.SessionFee(80m, H(10), H(11)));
            Assert.AreEqual(120m, SchedulePolicy.SessionFee(80m, H(10), H(11.5)));
            Assert.AreEqual(0m, SchedulePolicy.SessionFee(0m, H(10), H(12)));
        }

        [TestMethod]
        public void CancellationCutoff()
        {
            Assert.IsTrue(SchedulePolicy.IsBeforeCutoff(Now.Date.AddDays(1), H(9), Now, 12));
            Assert.IsFalse(SchedulePolicy.IsBeforeCutoff(Now.Date, H(20), Now, 12), "11 h away");
            Assert.IsTrue(SchedulePolicy.IsBeforeCutoff(Now.Date, H(21), Now, 12), "exactly 12 h away");
        }

        [TestMethod]
        public void WeekStartsOnMonday()
        {
            Assert.AreEqual(new DateTime(2026, 10, 5), SchedulePolicy.WeekStart(new DateTime(2026, 10, 11))); // Sunday
            Assert.AreEqual(new DateTime(2026, 10, 5), SchedulePolicy.WeekStart(new DateTime(2026, 10, 5)));  // Monday
        }

        [TestMethod]
        public void Utilisation()
        {
            Assert.AreEqual(78m, SchedulePolicy.UtilisationPercent(7.8m, 10m));
            Assert.AreEqual(0m, SchedulePolicy.UtilisationPercent(3m, 0m), "no published hours → 0, not divide-by-zero");
            Assert.AreEqual(100m, SchedulePolicy.UtilisationPercent(12m, 10m), "capped at 100");
        }
    }

    [TestClass]
    [TestCategory("Unit")]
    public class RentalAndStockPolicyTests
    {
        private static readonly DateTime Today = new DateTime(2026, 10, 5);

        [TestMethod]
        public void DueDateAndOverdue()
        {
            Assert.AreEqual(Today.AddDays(7), RentalPolicy.DueDate(Today.AddHours(15), 7));
            Assert.IsFalse(RentalPolicy.IsOverdue(RentalStatus.Active, Today, Today), "due today is not overdue");
            Assert.IsTrue(RentalPolicy.IsOverdue(RentalStatus.Active, Today.AddDays(-1), Today));
            Assert.IsFalse(RentalPolicy.IsOverdue(RentalStatus.Returned, Today.AddDays(-9), Today));
        }

        [TestMethod]
        public void OverdueFineIsDaysTimesRate_CappedAtBookPrice()
        {
            Assert.AreEqual(3, RentalPolicy.DaysLate(Today, Today.AddDays(3)));
            Assert.AreEqual(0, RentalPolicy.DaysLate(Today, Today.AddDays(-2)), "early return");
            Assert.AreEqual(30m, RentalPolicy.OverdueFine(3, 10m, 400m));
            Assert.AreEqual(400m, RentalPolicy.OverdueFine(90, 10m, 400m), "never more than buying the book");
            Assert.AreEqual(0m, RentalPolicy.OverdueFine(0, 10m, 400m));
        }

        [TestMethod]
        public void RenewalRules()
        {
            Assert.IsNull(RentalPolicy.CheckRenewal(RentalStatus.Active, Today.AddDays(2), Today, 0, 2, 0));
            Assert.IsNotNull(RentalPolicy.CheckRenewal(RentalStatus.Active, Today.AddDays(-1), Today, 0, 2, 0), "overdue");
            Assert.IsNotNull(RentalPolicy.CheckRenewal(RentalStatus.Active, Today.AddDays(2), Today, 2, 2, 0), "limit reached");
            Assert.IsNotNull(RentalPolicy.CheckRenewal(RentalStatus.Active, Today.AddDays(2), Today, 0, 2, 1), "someone waiting");
            Assert.IsNotNull(RentalPolicy.CheckRenewal(RentalStatus.Returned, Today.AddDays(2), Today, 0, 2, 0));
        }

        [TestMethod]
        public void StockIsDerivedFromCopyStatuses()
        {
            var s = StockSummary.From(new[]
            {
                CopyStatus.Available, CopyStatus.Available, CopyStatus.Rented, CopyStatus.Rented, CopyStatus.Rented,
                CopyStatus.Sold, CopyStatus.Lost, CopyStatus.Damaged, CopyStatus.Reserved
            }, overdue: 1, reorderLevel: 3);
            Assert.AreEqual(2, s.Available);
            Assert.AreEqual(3, s.Rented);
            Assert.AreEqual(1, s.Overdue);
            Assert.AreEqual(6, s.InStock, "available + reserved + rented");
            Assert.IsTrue(s.IsLowStock);
            Assert.IsFalse(s.IsOutOfStock);
        }
    }

    [TestClass]
    [TestCategory("Unit")]
    public class PaymentPolicyTests
    {
        private static readonly DateTime Today = new DateTime(2026, 10, 5);

        [DataTestMethod]
        [DataRow("4111 1111 1111 1111", "Visa")]
        [DataRow("5555 5555 5555 4444", "Mastercard")]
        [DataRow("2223 0031 2200 3222", "Mastercard")]
        [DataRow("3782 822463 10005", "Amex")]
        public void ValidCardsPass(string number, string brand)
        {
            var cvv = brand == "Amex" ? "1234" : "123";
            var check = CardValidator.Check(number, "12/30", cvv, "T Student", Today);
            Assert.IsTrue(check.IsValid, check.Error);
            Assert.AreEqual(brand, check.Brand);
            Assert.AreEqual(4, check.Last4.Length);
        }

        [TestMethod]
        public void InvalidCardsFail()
        {
            Assert.IsFalse(CardValidator.Check("4111 1111 1111 1112", "12/30", "123", "T", Today).IsValid, "Luhn");
            Assert.IsFalse(CardValidator.Check("4111 1111 1111 1111", "09/26", "123", "T Student", Today).IsValid, "expired");
            Assert.IsTrue(CardValidator.Check("4111 1111 1111 1111", "10/26", "123", "T Student", Today).IsValid, "valid through the end of the expiry month");
            Assert.IsFalse(CardValidator.Check("4111 1111 1111 1111", "13/30", "123", "T Student", Today).IsValid, "bad month");
            Assert.IsFalse(CardValidator.Check("4111 1111 1111 1111", "12/30", "12", "T Student", Today).IsValid, "short CVV");
            Assert.IsFalse(CardValidator.Check("4111 1111 1111 1111", "12/30", "123", "", Today).IsValid, "no holder");
            Assert.IsFalse(CardValidator.Check("6011 1111 1111 1117", "12/30", "123", "T Student", Today).IsValid, "unsupported brand");
        }

        [TestMethod]
        public void AllocationMustMatchExactly()
        {
            Assert.IsNull(MoneyPolicy.CheckAllocation(165m, new[] { 80m, 85m }));
            Assert.IsNotNull(MoneyPolicy.CheckAllocation(160m, new[] { 80m, 85m }));
            Assert.IsNotNull(MoneyPolicy.CheckAllocation(0m, new decimal[0]));
            Assert.AreEqual(80m, MoneyPolicy.TutorShare(100m, 20m));
            Assert.AreEqual(100m, MoneyPolicy.TutorShare(100m, -5m), "commission clamped to 0–100");
        }
    }

    [TestClass]
    [TestCategory("Unit")]
    public class SecurityTests
    {
        [TestMethod]
        public void PasswordHashing_RoundTripsAndSalts()
        {
            var a = PasswordHasher.Create("Correct horse 1");
            var b = PasswordHasher.Create("Correct horse 1");
            Assert.IsTrue(PasswordHasher.Verify("Correct horse 1", a.Hash, a.Salt, a.Iterations));
            Assert.IsFalse(PasswordHasher.Verify("Correct horse 2", a.Hash, a.Salt, a.Iterations));
            Assert.IsFalse(a.Hash.SequenceEqual(b.Hash), "random salt per password");
            Assert.AreEqual(100000, a.Iterations);
            Assert.IsTrue(PasswordHasher.NeedsRehash(10000));
        }

        [TestMethod]
        public void PasswordRules()
        {
            Assert.IsNull(UserValidator.CheckPassword("abcdefg1", "abcdefg1"));
            Assert.IsNotNull(UserValidator.CheckPassword("abc1", "abc1"), "too short");
            Assert.IsNotNull(UserValidator.CheckPassword("abcdefgh", "abcdefgh"), "needs a digit");
            Assert.IsNotNull(UserValidator.CheckPassword("12345678", "12345678"), "needs a letter");
            Assert.IsNotNull(UserValidator.CheckPassword("abcdefg1", "abcdefg2"), "must match");
            Assert.IsTrue(UserValidator.IsValidCell("082 123 4567"));
            Assert.IsTrue(UserValidator.IsValidCell("+27821234567"));
            Assert.IsFalse(UserValidator.IsValidCell("12345"));
        }

        private static Actor As(params string[] roles) => new Actor(1, "u", roles);

        [TestMethod]
        public void RolePermissionMatrix()
        {
            Assert.IsTrue(RolePermissions.Has(As(RoleNames.Student), Permission.BookTutoring));
            Assert.IsFalse(RolePermissions.Has(As(RoleNames.Student), Permission.ManageBooks));
            Assert.IsFalse(RolePermissions.Has(As(RoleNames.Tutor), Permission.BookTutoring));
            Assert.IsTrue(RolePermissions.Has(As(RoleNames.Staff), Permission.VerifyTutors));
            Assert.IsFalse(RolePermissions.Has(As(RoleNames.Staff), Permission.ViewAllReports), "staff get limited reports only");
            Assert.IsTrue(RolePermissions.Has(As(RoleNames.Manager), Permission.VerifyTutors), "managers may verify tutors too");
            Assert.IsFalse(RolePermissions.Has(As(RoleNames.Manager), Permission.ViewAuditLogs));
            Assert.IsTrue(RolePermissions.Has(As(RoleNames.SystemAdmin), Permission.ViewAuditLogs));
            Assert.IsFalse(RolePermissions.Has(As(RoleNames.Supplier), Permission.ManagePayments));
            Assert.ThrowsException<AccessDeniedException>(() => RolePermissions.Demand(As(RoleNames.Student), Permission.ManageSettings));
        }

        [TestMethod]
        public void OnlyStudentsAndTutorsSelfRegister_CreatorsAreLimited()
        {
            CollectionAssert.AreEquivalent(new[] { RoleNames.Staff, RoleNames.Supplier, RoleNames.Student },
                RolePermissions.CreatableRoles(As(RoleNames.Manager)).ToArray());
            CollectionAssert.Contains(RolePermissions.CreatableRoles(As(RoleNames.SystemAdmin)).ToArray(), RoleNames.Manager);
            Assert.IsFalse(RolePermissions.CreatableRoles(As(RoleNames.SystemAdmin)).Contains(RoleNames.Tutor), "tutors always self-register and are verified");
            Assert.IsFalse(RolePermissions.CreatableRoles(As(RoleNames.Student)).Any());
        }
    }
}
