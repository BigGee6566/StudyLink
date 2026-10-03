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

        [DataTestMethod]
        [DataRow("S202600001", true, DisplayName = "S + 9 digits")]
        [DataRow("s202600001", true, DisplayName = "lower-case s is accepted (stored upper-case)")]
        [DataRow(" S202600001 ", true, DisplayName = "surrounding spaces are trimmed")]
        [DataRow("202600001", false, DisplayName = "no S")]
        [DataRow("2026000001", false, DisplayName = "10 digits without S")]
        [DataRow("S20260001", false, DisplayName = "only 8 digits")]
        [DataRow("S2026000011", false, DisplayName = "10 digits after S")]
        [DataRow("SA02600001", false, DisplayName = "letter among the digits")]
        [DataRow("Q202600001", false, DisplayName = "wrong prefix")]
        [DataRow("", false, DisplayName = "empty")]
        public void StudentNumberIsSAndNineDigits(string number, bool valid)
        {
            Assert.AreEqual(valid, UserValidator.IsValidStudentNumber(number));
        }

        [TestMethod]
        public void ImagesAreRecognisedByTheirBytes()
        {
            var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D };
            var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 1 };
            var webp = System.Text.Encoding.ASCII.GetBytes("RIFF\0\0\0\0WEBPVP8 ");
            Assert.AreEqual("image/png", ImageRules.Detect(png));
            Assert.AreEqual("image/jpeg", ImageRules.Detect(jpeg));
            Assert.AreEqual("image/webp", ImageRules.Detect(webp));
            Assert.IsNull(ImageRules.Detect(System.Text.Encoding.UTF8.GetBytes("<html><script>x</script>")), "a renamed HTML file is not an image");
            Assert.IsNotNull(ImageRules.Prepare(new byte[ImageRules.MaxUploadBytes + 1], ImageRules.AvatarSide).Error, "over 15 MB");
            Assert.IsNotNull(ImageRules.Prepare(png, ImageRules.AvatarSide).Error, "a PNG header with no picture behind it");
        }

        [TestMethod]
        public void PhoneSizedPhoto_IsShrunkTurnedUprightAndStrippedOfMetadata()
        {
            var photo = TestImages.Jpeg(4000, 3000, noisy: true, exifOrientation: 6);
            Assert.IsTrue(photo.Length > ImageRules.MaxBytes, "the sample is larger than what is stored, like a real phone photo");

            var prepared = ImageRules.Prepare(photo, ImageRules.AvatarSide);
            Assert.IsNull(prepared.Error, prepared.Error);
            Assert.AreEqual("image/jpeg", prepared.ContentType);
            Assert.IsTrue(prepared.Data.Length <= ImageRules.MaxBytes);
            Assert.AreEqual(new System.Drawing.Size(384, 512), TestImages.SizeOf(prepared.Data), "orientation 6 = rotate 90°, then fit 512");
            Assert.IsFalse(TestImages.HasExif(prepared.Data), "camera metadata is not kept");
        }

        [TestMethod]
        public void SmallPng_IsKeptAsSent_AndWebpMustAlreadyBeSmall()
        {
            var png = TestImages.Png(200, 120);
            var prepared = ImageRules.Prepare(png, ImageRules.AvatarSide);
            Assert.AreEqual("image/png", prepared.ContentType);
            CollectionAssert.AreEqual(png, prepared.Data);

            var bigWebp = new byte[ImageRules.MaxBytes + 1];
            System.Text.Encoding.ASCII.GetBytes("RIFF\0\0\0\0WEBPVP8 ").CopyTo(bigWebp, 0);
            Assert.IsNotNull(ImageRules.Prepare(bigWebp, ImageRules.AvatarSide).Error, "WebP cannot be shrunk on .NET Framework");
        }
    }

    [TestClass]
    [TestCategory("Unit")]
    public class AvatarPolicyTests
    {
        [DataTestMethod]
        [DataRow(AvatarViewer.Public, AvatarOwner.VerifiedTutor, false, true, DisplayName = "public sees the tutor directory")]
        [DataRow(AvatarViewer.Public, AvatarOwner.UnverifiedTutor, false, false, DisplayName = "public: unverified tutor hidden")]
        [DataRow(AvatarViewer.Public, AvatarOwner.Student, false, false, DisplayName = "public: student hidden")]
        [DataRow(AvatarViewer.Student, AvatarOwner.Student, false, false, DisplayName = "student: other student hidden")]
        [DataRow(AvatarViewer.Student, AvatarOwner.VerifiedTutor, false, true, DisplayName = "student sees tutors")]
        [DataRow(AvatarViewer.Student, AvatarOwner.Staff, false, false, DisplayName = "student: staff hidden")]
        [DataRow(AvatarViewer.Tutor, AvatarOwner.Student, true, true, DisplayName = "tutor sees their own students")]
        [DataRow(AvatarViewer.Tutor, AvatarOwner.Student, false, false, DisplayName = "tutor: other students hidden")]
        [DataRow(AvatarViewer.Tutor, AvatarOwner.Staff, false, false, DisplayName = "tutor: staff hidden")]
        [DataRow(AvatarViewer.Staff, AvatarOwner.Student, false, true, DisplayName = "staff sees students")]
        [DataRow(AvatarViewer.Staff, AvatarOwner.Staff, false, true, DisplayName = "staff sees staff")]
        [DataRow(AvatarViewer.Staff, AvatarOwner.Manager, false, false, DisplayName = "staff: manager hidden")]
        [DataRow(AvatarViewer.Staff, AvatarOwner.Admin, false, false, DisplayName = "staff: admin hidden")]
        [DataRow(AvatarViewer.Manager, AvatarOwner.Manager, false, true, DisplayName = "manager sees managers")]
        [DataRow(AvatarViewer.Manager, AvatarOwner.Admin, false, false, DisplayName = "manager: admin hidden")]
        [DataRow(AvatarViewer.Admin, AvatarOwner.Admin, false, true, DisplayName = "admin sees everyone")]
        [DataRow(AvatarViewer.Supplier, AvatarOwner.Student, false, false, DisplayName = "supplier: student hidden")]
        public void AccessMatrix(AvatarViewer viewer, AvatarOwner owner, bool tutorsOwner, bool expected)
        {
            Assert.AreEqual(expected, AvatarPolicy.CanView(false, new[] { viewer }, owner, tutorsOwner));
        }

        [TestMethod]
        public void EveryoneSeesTheirOwnPhoto_AndOwnerIsTheirMostPrivateRole()
        {
            Assert.IsTrue(AvatarPolicy.CanView(true, new[] { AvatarViewer.Public }, AvatarOwner.Admin, false));
            Assert.AreEqual(AvatarOwner.Manager, AvatarPolicy.OwnerKind(new[] { RoleNames.Staff, RoleNames.Manager }, false));
            Assert.AreEqual(AvatarOwner.Student, AvatarPolicy.OwnerKind(new[] { RoleNames.Tutor, RoleNames.Student }, true));
            Assert.AreEqual(AvatarOwner.UnverifiedTutor, AvatarPolicy.OwnerKind(new[] { RoleNames.Tutor }, false));
            Assert.IsTrue(AvatarPolicy.IsPublic(AvatarOwner.VerifiedTutor));
            Assert.IsFalse(AvatarPolicy.IsPublic(AvatarOwner.Student));
            CollectionAssert.AreEqual(new[] { AvatarViewer.Public }, AvatarPolicy.ViewerKinds(null).ToArray(), "anonymous visitors are Public");
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
