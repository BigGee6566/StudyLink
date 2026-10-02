using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StudyLink.Data.Entities;
using StudyLink.Data.Seed;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Validation;

namespace StudyLink.Tests.Integration
{
    [TestClass]
    [TestCategory("Integration")]
    public class BooksAndOperationsTests
    {
        [TestMethod]
        public void LastCopy_TwoStudentsRentAtOnce_OnlyOneGetsIt()
        {
            using (var w = new World())
            {
                var book = w.Book(w.NewManager(), copiesAtAlice: 1);
                var students = Enumerable.Range(0, 5).Select(_ => w.NewStudent()).ToList();
                var campus = w.Campus();
                var gate = new ManualResetEventSlim(false);
                var tasks = students.Select(s => Task.Run(() =>
                {
                    using (var own = w.NewServices())
                    {
                        gate.Wait();
                        return own.Rentals.Reserve(s, book, campus);
                    }
                })).ToList();
                gate.Set();
                Task.WaitAll(tasks.Cast<Task>().ToArray());

                Assert.AreEqual(1, tasks.Count(t => t.Result.Succeeded), string.Join(" | ", tasks.Select(t => t.Result.Error)));
                w.Refresh();
                Assert.AreEqual(0, w.S.Db.BookCopies.Count(c => c.BookId == book && c.Status == CopyStatus.Available), "stock decremented");
            }
        }

        [TestMethod]
        public void RentPayReturnLate_FineCreatedPaidAndStockBack()
        {
            using (var w = new World())
            {
                var manager = w.NewManager();
                var book = w.Book(manager, copiesAtAlice: 2, salePrice: 400m, weekly: 45m, lateFee: 10m);
                var student = w.NewStudent();

                var rental = w.S.Rentals.Reserve(student, book, w.Campus());
                Assert.IsTrue(rental.Succeeded, rental.Error);
                var fee = w.FeeFor(rentalId: rental.Value);
                Assert.AreEqual(45m, w.S.Db.Fees.Find(fee).Amount, "7-day period = one week's fee (SC12)");
                Assert.IsTrue(w.S.Payments.Pay(student, new List<int> { fee }, World.Card(), Guid.NewGuid()).Succeeded);

                w.Refresh();
                var active = w.S.Db.Rentals.Find(rental.Value);
                Assert.AreEqual(RentalStatus.Active, active.Status);
                Assert.AreEqual(w.Clock.Today.AddDays(7), active.DueDate);
                Assert.AreEqual(CopyStatus.Rented, w.S.Db.BookCopies.Find(active.CopyId).Status);

                // Returned 3 days late in fair condition.
                w.Clock.Advance(TimeSpan.FromDays(10));
                var staff = w.NewStaff();
                var ret = w.S.Rentals.ProcessReturn(staff, new ReturnRequest { RentalId = rental.Value, Condition = CopyCondition.Fair });
                Assert.IsTrue(ret.Succeeded, ret.Error);
                Assert.AreEqual(3, ret.Value.DaysLate);
                Assert.AreEqual(30m, ret.Value.OverdueFine);

                w.Refresh();
                Assert.AreEqual(CopyStatus.Available, w.S.Db.BookCopies.Find(active.CopyId).Status, "stock incremented on return");
                var statement = w.S.Fees.Statement(student);
                Assert.AreEqual(30m, statement.Outstanding);

                var fineFee = statement.Unpaid.Single().FeeId;
                Assert.IsTrue(w.S.Payments.Pay(student, new List<int> { fineFee }, World.Card(), Guid.NewGuid()).Succeeded);
                w.Refresh();
                Assert.AreEqual(FineStatus.Paid, w.S.Db.Fines.Single(f => f.RentalId == rental.Value).Status);
                Assert.AreEqual(0m, w.S.Fees.Statement(student).Outstanding);
            }
        }

        [TestMethod]
        public void RenewalRules_MaxTwoAndNotWhenSomeoneWaits()
        {
            using (var w = new World())
            {
                var book = w.Book(w.NewManager(), copiesAtAlice: 1, weekly: 0m);
                var student = w.NewStudent();
                var rental = w.S.Rentals.Reserve(student, book, w.Campus()).Value; // free rental: active at once

                Assert.IsTrue(w.S.Rentals.Renew(student, rental).Succeeded);
                Assert.IsTrue(w.S.Rentals.Renew(student, rental).Succeeded);
                var third = w.S.Rentals.Renew(student, rental);
                Assert.IsFalse(third.Succeeded);
                StringAssert.Contains(third.Error, "maximum");

                var other = w.NewStudent();
                var rental2Book = w.Book(w.NewManager(), copiesAtAlice: 1, weekly: 0m);
                var r2 = w.S.Rentals.Reserve(student, rental2Book, w.Campus()).Value;
                Assert.IsTrue(w.S.Catalogue.JoinWaitlist(other, rental2Book, w.Campus()).Succeeded);
                var blocked = w.S.Rentals.Renew(student, r2);
                Assert.IsFalse(blocked.Succeeded);
                StringAssert.Contains(blocked.Error, "waiting");
            }
        }

        [TestMethod]
        public void DoubleSubmittedCheckout_ChargesOnce()
        {
            using (var w = new World())
            {
                var book = w.Book(w.NewManager(), copiesAtAlice: 1, salePrice: 250m);
                var student = w.NewStudent();
                var sale = w.S.Rentals.Buy(student, book, w.Campus());
                Assert.IsTrue(sale.Succeeded, sale.Error);
                var fee = w.FeeFor(saleId: sale.Value);
                var token = Guid.NewGuid();

                var first = w.S.Payments.Pay(student, new List<int> { fee }, World.Card(), token);
                var second = w.S.Payments.Pay(student, new List<int> { fee }, World.Card(), token);
                Assert.IsTrue(first.Succeeded && second.Succeeded);
                Assert.AreEqual(first.Value, second.Value);
                w.Refresh();
                Assert.AreEqual(1, w.S.Db.Payments.Count(p => p.Student.UserId == student.UserId && p.Status == PaymentStatus.Completed));
                Assert.AreEqual(CopyStatus.Sold, w.S.Db.Sales.Include("Copy").Single(s => s.SaleId == sale.Value).Copy.Status);
            }
        }

        [TestMethod]
        public void DeclinedCard_RecordsFailure_FeeStaysUnpaid()
        {
            using (var w = new World())
            {
                var book = w.Book(w.NewManager(), copiesAtAlice: 1);
                var student = w.NewStudent();
                var rental = w.S.Rentals.Reserve(student, book, w.Campus()).Value;
                var fee = w.FeeFor(rentalId: rental);

                var declined = w.S.Payments.Pay(student, new List<int> { fee }, World.Card("4000 0000 0000 0002"), Guid.NewGuid());
                Assert.IsFalse(declined.Succeeded);
                var badLuhn = w.S.Payments.Pay(student, new List<int> { fee }, World.Card("4111 1111 1111 1112"), Guid.NewGuid());
                Assert.IsFalse(badLuhn.Succeeded);

                w.Refresh();
                Assert.AreEqual(FeeStatus.Unpaid, w.S.Db.Fees.Find(fee).Status);
                Assert.IsTrue(w.S.Db.Payments.Any(p => p.Student.UserId == student.UserId && p.Status == PaymentStatus.Failed));
                Assert.IsFalse(w.S.Db.Payments.Any(p => p.CardLast4 != null && p.CardLast4.Length != 4), "only the last four digits are stored");
            }
        }

        [TestMethod]
        public void SupplierFlow_OrderDispatchReceive_StockRisesOnlyOnReceipt()
        {
            using (var w = new World())
            {
                var manager = w.NewManager();
                var supplier = w.NewSupplier();
                var supplierId = w.S.Db.Suppliers.Single(s => s.UserId == supplier.UserId).SupplierId;
                var book = w.Book(manager, copiesAtAlice: 0);

                var order = w.S.SupplierOrders.Create(manager, supplierId, w.Clock.Today.AddDays(7), null,
                    new List<NewOrderLine> { new NewOrderLine { BookId = book, CampusId = w.Campus(), Quantity = 5, UnitCost = 300m } });
                Assert.IsTrue(order.Succeeded, order.Error);
                Assert.IsTrue(w.S.SupplierOrders.Acknowledge(supplier, order.Value).Succeeded);

                var lineId = w.S.Db.SupplierOrderLines.Single(l => l.OrderId == order.Value).LineId;
                var dispatch = w.S.SupplierOrders.Dispatch(supplier, order.Value, new Dictionary<int, int> { { lineId, 4 } }, "Box 1");
                Assert.IsTrue(dispatch.Succeeded, dispatch.Error);
                Assert.AreEqual(0, w.S.Db.BookCopies.Count(c => c.BookId == book), "no stock until the hub receives");

                var deliveryLine = w.S.Db.DeliveryLines.Single(l => l.DeliveryId == dispatch.Value).DeliveryLineId;
                Assert.IsTrue(w.S.SupplierOrders.Receive(w.NewStaff(), dispatch.Value, new Dictionary<int, int> { { deliveryLine, 3 } }, "1 damaged in transit").Succeeded);

                w.Refresh();
                Assert.AreEqual(3, w.S.Db.BookCopies.Count(c => c.BookId == book && c.Status == CopyStatus.Available));
                Assert.AreEqual(SupplierOrderStatus.PartiallyDelivered, w.S.Db.SupplierOrders.Find(order.Value).Status);

                var pdf = Encoding.ASCII.GetBytes("%PDF-1.4 test invoice");
                Assert.IsFalse(w.S.SupplierOrders.UploadInvoice(supplier, order.Value, "INV-1", 900m, "invoice.exe", pdf).Succeeded, "PDF only");
                var invoice = w.S.SupplierOrders.UploadInvoice(supplier, order.Value, "INV-1", 900m, "invoice.pdf", pdf);
                Assert.IsTrue(invoice.Succeeded, invoice.Error);
                Assert.IsTrue(w.S.SupplierOrders.ReviewInvoice(manager, invoice.Value, InvoiceStatus.Approved, null).Succeeded);
                Assert.IsTrue(w.S.SupplierOrders.ReviewInvoice(manager, invoice.Value, InvoiceStatus.Paid, null).Succeeded);
                Assert.AreEqual(InvoiceStatus.Paid, w.S.SupplierOrders.MyInvoices(supplier).Single().Status);
            }
        }

        [TestMethod]
        public void Reports_ReflectRealData()
        {
            using (var w = new World())
            {
                var manager = w.NewManager();
                var book = w.Book(manager, copiesAtAlice: 1, weekly: 0m, lateFee: 5m);
                var student = w.NewStudent();
                var rental = w.S.Rentals.Reserve(student, book, w.Campus()).Value;
                w.Clock.Advance(TimeSpan.FromDays(9)); // 2 days overdue

                var report = w.S.Reports.Run(manager, "overdue", null, null);
                var row = report.Table.Rows.Cast<System.Data.DataRow>().Single(r => (string)r["Rental"] == w.S.Db.Rentals.Find(rental).Reference);
                Assert.AreEqual("2", row["Days overdue"].ToString());

                var staff = w.NewStaff();
                Assert.IsFalse(w.S.Reports.Available(staff).Any(d => d.Key == "revenue"), "staff see limited reports");
                Assert.ThrowsException<AccessDeniedException>(() => w.S.Reports.Run(staff, "revenue", null, null));
                StringAssert.Contains(w.S.Reports.ToCsv(report), "Days overdue");
            }
        }

        /// <summary>
        /// Every dashboard query must translate to SQL (EF6 rejects things like DateTime.AddDays inside a
        /// query at run time only), and every report must run for a manager.
        /// </summary>
        [TestMethod]
        public void AllDashboardsAndReports_BuildFromLiveData()
        {
            using (var w = new World())
            {
                var manager = w.NewManager();
                w.Book(manager, copiesAtAlice: 1, weekly: 10m, lateFee: 5m);
                var student = w.NewStudent();
                var tutor = w.NewTutor();

                Assert.IsNotNull(w.S.Dashboards.Student(student));
                Assert.IsNotNull(w.S.Dashboards.Tutor(tutor));
                Assert.IsNotNull(w.S.Dashboards.Staff(w.NewStaff()));
                Assert.IsNotNull(w.S.Dashboards.Manager(manager));
                Assert.IsNotNull(w.S.Dashboards.Supplier(w.NewSupplier()));
                Assert.IsNotNull(w.S.Dashboards.Admin(w.Admin()));

                foreach (var report in w.S.Reports.Available(manager))
                    Assert.IsNotNull(w.S.Reports.Run(manager, report.Key, w.Clock.Today.AddDays(-30), w.Clock.Today).Table, report.Key);
            }
        }
    }

    [TestClass]
    [TestCategory("Integration")]
    public class CoverRuleTests
    {
        private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52 };

        [TestMethod]
        public void NewBookNeedsARealCoverImage_UnlessTheSeederSaysOtherwise()
        {
            using (var w = new World())
            {
                var manager = w.NewManager();
                BookEdit Book(byte[] cover) => new BookEdit
                {
                    Isbn = World.Isbn13("978" + World.NextDigits(9)), Title = "Cover Rule " + World.Suffix(), Authors = "Test Author",
                    CategoryId = w.S.Db.Categories.First().CategoryId, SalePrice = 300m, WeeklyRentalFee = 30m, LateFeePerDay = 5m,
                    ReorderLevel = 1, IsRentable = true, IsForSale = true, IsActive = true, CoverImage = cover, CoverContentType = "image/png"
                };

                var none = w.S.Inventory.Save(manager, Book(null));
                Assert.IsFalse(none.Succeeded, "a manager cannot add a title without its cover");
                Assert.AreEqual(StudyLink.Services.Implementations.InventoryService.CoverRequired, none.Error);

                var fake = w.S.Inventory.Save(manager, Book(Encoding.UTF8.GetBytes("<script>alert(1)</script> pretending to be a png")));
                Assert.IsFalse(fake.Succeeded, "the claimed content type is not trusted; the bytes are checked");

                var real = w.S.Inventory.Save(manager, Book(Png));
                Assert.IsTrue(real.Succeeded, real.Error);
                Assert.AreEqual("image/png", w.S.Db.Books.Find(real.Value).CoverContentType);

                Assert.IsTrue(w.S.Inventory.Save(manager, Book(null), allowMissingCover: true).Succeeded, "seeder/test override");
            }
        }
    }

    [TestClass]
    [TestCategory("Integration")]
    public class AccountTests
    {
        [TestMethod]
        public void DuplicateUsernameRejected_AndTutorStartsPending()
        {
            using (var w = new World())
            {
                var student = w.NewStudent();
                var clash = w.S.Registration.RegisterStudent(new StudentRegistration
                {
                    UserName = student.UserName, FirstName = "A", LastName = "B", Email = "x_" + World.Suffix() + "@t.local", CellNumber = "0821234567",
                    CampusId = w.Campus(), StudentNumber = World.StudentNumber(), Password = World.Password, ConfirmPassword = World.Password, AcceptedTerms = true
                }, "dup-" + World.Suffix());
                Assert.IsFalse(clash.Succeeded);

                w.NewStaff(); // someone who can verify tutors must exist to be notified, whatever ran before
                var tutor = w.NewTutor(verify: false);
                Assert.AreEqual(TutorVerificationStatus.PendingVerification, w.S.Db.Tutors.Single(t => t.UserId == tutor.UserId).VerificationStatus);
                Assert.IsTrue(w.S.Db.Notifications.Any(n => n.Type == NotificationType.TutorRegistrationSubmitted), "staff were told");
            }
        }

        [TestMethod]
        public void StudentNumberMustBeSAndNineDigits_OnRegistrationAndStaffCapture()
        {
            using (var w = new World())
            {
                StudentRegistration Form(string studentNumber) => new StudentRegistration
                {
                    UserName = "num_" + World.Suffix(), FirstName = "Ayanda", LastName = "Student", Email = "num_" + World.Suffix() + "@t.local",
                    CellNumber = "0821234567", CampusId = w.Campus(), StudentNumber = studentNumber, Password = World.Password,
                    ConfirmPassword = World.Password, AcceptedTerms = true
                };

                var nineDigits = w.S.Registration.RegisterStudent(Form("202600001"), "num-" + World.Suffix());
                Assert.IsFalse(nineDigits.Succeeded, "the old 9-digit format is refused");
                Assert.AreEqual(UserValidator.StudentNumberMessage, nineDigits.Error);

                var number = World.StudentNumber();
                var ok = w.S.Registration.RegisterStudent(Form(number.ToLowerInvariant()), "num-" + World.Suffix());
                Assert.IsTrue(ok.Succeeded, ok.Error);
                Assert.AreEqual(number, w.S.Db.Students.Single(s => s.UserId == ok.Value).StudentNumber, "stored upper-case");

                var captured = w.S.Users.CreateAccount(w.NewStaff(), new CreateAccountRequest
                {
                    Role = RoleNames.Student, UserName = "cap_" + World.Suffix(), FirstName = "Lindo", LastName = "Captured",
                    Email = "cap_" + World.Suffix() + "@t.local", CellNumber = "0841234567", CampusId = w.Campus(), StudentNumber = "S12345"
                }, "https://test/reset?token=");
                Assert.IsFalse(captured.Succeeded, "staff capture applies the same rule");
                Assert.AreEqual(UserValidator.StudentNumberMessage, captured.Error);
            }
        }

        [TestMethod]
        public void FiveBadPasswordsLockTheAccount_WithTheSameMessage()
        {
            using (var w = new World())
            {
                var student = w.NewStudent();
                for (var i = 0; i < 5; i++)
                    Assert.IsFalse(w.S.Auth.SignIn(student.UserName, "wrong-password-1", "t").Succeeded);

                var locked = w.S.Auth.SignIn(student.UserName, World.Password, "t");
                Assert.IsFalse(locked.Succeeded, "locked even with the right password");
                Assert.AreEqual("Incorrect username or password.", locked.Error, "AT-05: no account enumeration");

                w.Clock.Advance(TimeSpan.FromMinutes(16));
                Assert.IsTrue(w.S.Auth.SignIn(student.UserName, World.Password, "t").Succeeded);
            }
        }

        [TestMethod]
        public void ManagerMayCreateOnlyStaffSuppliersAndStudents()
        {
            using (var w = new World())
            {
                var manager = w.NewManager();
                w.NewAccount(RoleNames.Staff, manager);
                Assert.ThrowsException<AccessDeniedException>(() => w.NewAccount(RoleNames.SystemAdmin, manager));
                Assert.ThrowsException<AccessDeniedException>(() => w.NewAccount(RoleNames.Manager, manager));
                Assert.IsTrue(w.Email.Sent.Any(e => e.Item2.Contains("account is ready")), "new staff get a set-password link, never a password");
            }
        }

        [TestMethod]
        public void PasswordResetNeedsStaffApproval_ThenTokenIsSingleUse()
        {
            using (var w = new World())
            {
                var student = w.NewStudent();
                var asked = w.S.PasswordResets.Request(student.UserName, "Forgot it", "reset-" + World.Suffix());
                Assert.IsTrue(asked.Succeeded, asked.Error);
                Assert.IsTrue(w.S.PasswordResets.Request("nobody-" + World.Suffix(), null, "reset-" + World.Suffix()).Succeeded, "no enumeration");

                var staff = w.NewStaff();
                var request = w.S.PasswordResets.List(staff, RequestStatus.Pending).First(r => r.UserId == student.UserId);
                var approved = w.S.PasswordResets.Approve(staff, request.RequestId, "Student card seen", "https://test/reset?token=");
                Assert.IsTrue(approved.Succeeded, approved.Error);

                var mail = w.Email.Sent.Single(e => e.Item2.Contains("Reset your StudyLink password") && e.Item3.Contains("https://test/reset?token="));
                var token = Uri.UnescapeDataString(System.Text.RegularExpressions.Regex.Match(mail.Item3, "token=([^\"<]+)").Groups[1].Value);
                var reset = w.S.PasswordResets.ResetWithToken(token, "NewPassw0rd", "NewPassw0rd", "t");
                Assert.IsTrue(reset.Succeeded, reset.Error + " token=" + token);
                Assert.IsFalse(w.S.PasswordResets.ResetWithToken(token, "OtherPassw0rd", "OtherPassw0rd", "t").Succeeded, "single use");
                Assert.IsTrue(w.S.Auth.SignIn(student.UserName, "NewPassw0rd", "t").Succeeded);
            }
        }

        [TestMethod]
        public void LastAdminCannotBeDeactivated_AndStudentCannotReachAdminServices()
        {
            using (var w = new World())
            {
                var admin = w.Admin();
                var other = w.NewAccount(RoleNames.SystemAdmin);
                // Leave exactly these two admins active (the test database is shared by all tests).
                foreach (var ur in w.S.Db.UserRoles.Where(x => x.Role.Name == RoleNames.SystemAdmin && x.UserId != admin.UserId && x.UserId != other.UserId).ToList())
                    w.S.Db.Users.Find(ur.UserId).IsActive = false;
                w.S.Db.SaveChanges();

                Assert.IsFalse(w.S.Users.SetActive(admin, admin.UserId, false, "self").Succeeded, "no self-deactivation");
                Assert.IsTrue(w.S.Users.SetActive(other, admin.UserId, false, "left the hub").Succeeded);
                var last = w.S.Users.SetActive(admin, other.UserId, false, "oops");
                Assert.IsFalse(last.Succeeded);
                StringAssert.Contains(last.Error, "last active System Admin");
                var student = w.NewStudent();
                Assert.ThrowsException<AccessDeniedException>(() => w.S.Users.Search(student, new UserFilter()));
                Assert.ThrowsException<AccessDeniedException>(() => w.S.Settings.Update(student, SettingKeys.MaxRenewals, "9"));
                Assert.ThrowsException<AccessDeniedException>(() => w.S.Audit.Search(student, new AuditFilter()));
            }
        }
    }
}
