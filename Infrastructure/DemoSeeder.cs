using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using StudyLink.Data.Entities;
using StudyLink.Data.Seed;
using StudyLink.Services;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Policies;
using StudyLink.Services.Security;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>
    /// Development-only demo data (ported idea: TutorConnect SeedRunner). Runs once, on start-up of a
    /// DEBUG build, when App_Data\local.settings.config sets Seed:Demo=true and Seed:DevPassword.
    /// The password never appears in source control. Uses the real services wherever a rule is
    /// involved, and writes history (past sessions, an overdue loan) directly because the services
    /// rightly refuse to create things in the past.
    /// </summary>
    public static class DemoSeeder
    {
        public static void RunIfRequested()
        {
#if DEBUG
            if (!string.Equals(ConfigurationManager.AppSettings["Seed:Demo"], "true", StringComparison.OrdinalIgnoreCase)) return;
            var password = ConfigurationManager.AppSettings["Seed:DevPassword"];
            if (string.IsNullOrWhiteSpace(password)) return;

            using (var s = AppServices.Create())
            {
                if (s.Db.Users.Any(u => u.UserName == "admin")) return; // already seeded
                new Run(s, password).All();
            }
#endif
        }

        /// <summary>Brings demo data seeded by an older build up to date. Same conditions as the seed itself.</summary>
        public static void CorrectIfRequested()
        {
#if DEBUG
            if (!string.Equals(ConfigurationManager.AppSettings["Seed:Demo"], "true", StringComparison.OrdinalIgnoreCase)) return;
            using (var s = AppServices.Create())
                CorrectDemoData(s.Db);
#endif
        }

        /// <summary>The demo catalogue: ISBN, title, authors, edition, category, subject, sale price, weekly fee, late fee, reorder level.</summary>
        private static readonly (string Isbn, string Title, string Authors, string Edition, string Category, string Subject, decimal Price, decimal Weekly, decimal Late, int Reorder)[] BookSpecs =
        {
            ("9780273655879", "Financial Accounting for Non-Specialists", "Peter Atrill, Eddie McLaney", "3rd", "Accounting & Finance", "Accounting", 650m, 45m, 10m, 4),
            ("9781292409207", "Business Statistics", "Paul Newbold, William Carlson", "9th", "Mathematics & Statistics", "Statistics", 720m, 50m, 10m, 3),
            ("9780324665284", "Principles of Information Systems", "Ralph Stair, George Reynolds", "9th", "Computer Science & IS", "Information Systems", 890m, 60m, 12m, 3),
            ("9780534362980", "Calculus: Early Transcendentals", "James Stewart", "4th", "Mathematics & Statistics", "Mathematics", 950m, 65m, 12m, 2),
            ("9781305585126", "Principles of Economics", "N. Gregory Mankiw", "8th", "Economics", "Economics", 780m, 55m, 10m, 3),
            ("9780190419264", "Introduction to South African Law", "Christa van Wyk (ed.)", "2nd", "Law", "Law", 540m, 40m, 8m, 2),
            ("9781444128086", "Psychology: The Science of Mind and Behaviour", "Richard Gross", "6th", "Humanities & Social Sciences", "Psychology", 610m, 45m, 8m, 2),
            ("9780199544813", "Business Management: A Contemporary Approach", "Marius Botha et al.", "3rd", "Business & Management", "Business Management", 480m, 35m, 8m, 3)
        };

        /// <summary>
        /// The first demo catalogue used invented ISBNs that belong to other books, so a real cover could
        /// never match. These titles are now real editions (covers ship in Images/covers); a database
        /// seeded earlier has its old rows renamed. Idempotent: an old ISBN is gone after the first run.
        /// </summary>
        private static readonly (string Old, string New)[] ReplacedIsbns =
        {
            ("9780273778790", "9780273655879"), ("9781337902571", "9780324665284"), ("9780134093413", "9780534362980"),
            ("9781319120054", "9781305585126"), ("9781473758537", "9781444128086")
        };

        /// <summary>Demo students seeded before student numbers became S + 9 digits.</summary>
        private static readonly (string UserName, string Number)[] DemoStudentNumbers =
        {
            ("student.thando", "S202400101"), ("student.olona", "S202300456"), ("student.aphiwe", "S202200789")
        };

        private static void CorrectDemoData(StudyLink.Data.StudyLinkDbContext db)
        {
            foreach (var (userName, number) in DemoStudentNumbers)
            {
                var student = db.Students.FirstOrDefault(s => s.User.UserName == userName);
                if (student != null && student.StudentNumber != number && !db.Students.Any(s => s.StudentNumber == number))
                    student.StudentNumber = number;
            }
            foreach (var (old, isbn) in ReplacedIsbns)
            {
                var book = db.Books.FirstOrDefault(b => b.Isbn == old);
                if (book == null || db.Books.Any(b => b.Isbn == isbn)) continue;
                var spec = BookSpecs.Single(b => b.Isbn == isbn);
                book.Isbn = spec.Isbn;
                book.Title = spec.Title;
                book.Authors = spec.Authors;
                book.Edition = spec.Edition;
            }
            db.SaveChanges();
        }

        private sealed class Run
        {
            private readonly StudyLinkServices _s;
            private readonly string _pw;
            private readonly DateTime _now = DateTime.Now;
            private Actor _admin, _manager, _staff;

            public Run(StudyLinkServices s, string password)
            {
                _s = s;
                _pw = password;
            }

            private int Campus(string name) => _s.Db.Campuses.Single(c => c.Name == name).CampusId;
            private int Subject(string name) => _s.Db.Subjects.Single(x => x.Name == name).SubjectId;
            private int Level(string name) => _s.Db.StudyLevels.Single(x => x.Name == name).StudyLevelId;
            private Actor ActorOf(int userId) => new Actor(userId, _s.Db.Users.Find(userId).UserName, _s.Auth.RolesOf(userId), "seed");

            private static void Ok(ServiceResult r, string what)
            {
                if (!r.Succeeded) throw new InvalidOperationException("Demo seed failed at " + what + ": " + r.Error);
            }

            public void All()
            {
                Admin();
                _manager = Staffer(RoleNames.Manager, "manager", "Nomsa", "Jacobs", "MG1001", "Hub Manager");
                _staff = Staffer(RoleNames.Staff, "staff", "Lwazi", "Ndlovu", "ST2001", "Front Desk Assistant");
                var supplier = Supplier();
                var tutors = Tutors();
                var students = Students();
                var books = Books(supplier);
                Activity(tutors, students, books, supplier);
                s_log();
            }

            private void s_log() => _s.Audit.Record(_admin, "DemoDataSeeded", "System", "demo", details: "Demo accounts and sample data created");

            private void Admin()
            {
                var details = new PersonalDetails
                {
                    UserName = "admin", FirstName = "Sinovuyo", LastName = "Hletshana", Email = "admin@studylink.local",
                    CellNumber = "0400000001", CampusId = Campus("Alice"), Gender = Gender.PreferNotToSay
                };
                Ok(_s.Auth.CreateFirstAdmin(details, _pw, _pw, "seed"), "admin");
                _admin = ActorOf(_s.Db.Users.Single(u => u.UserName == "admin").UserId);
            }

            private Actor Staffer(string role, string userName, string first, string last, string empNo, string title)
            {
                var r = _s.Users.CreateAccount(_admin, new CreateAccountRequest
                {
                    Role = role, UserName = userName, FirstName = first, LastName = last, Email = userName + "@studylink.local",
                    CellNumber = "0400000002", CampusId = Campus("Alice"), EmployeeNumber = empNo, JobTitle = title
                }, "https://localhost/Account/ResetPassword.aspx?token=");
                Ok(r, userName);
                SetPassword(r.Value);
                return ActorOf(r.Value);
            }

            /// <summary>Accounts created by staff normally receive a set-password link; demo accounts get the dev password.</summary>
            private void SetPassword(int userId)
            {
                var user = _s.Db.Users.Find(userId);
                var hashed = PasswordHasher.Create(_pw);
                user.PasswordHash = hashed.Hash;
                user.PasswordSalt = hashed.Salt;
                user.HashIterations = hashed.Iterations;
                user.MustChangePassword = false;
                _s.Db.SaveChanges();
            }

            private Actor Supplier()
            {
                var r = _s.Users.CreateAccount(_manager, new CreateAccountRequest
                {
                    Role = RoleNames.Supplier, UserName = "supplier", FirstName = "Thabo", LastName = "Mokoena",
                    Email = "orders@vanschaik-demo.local", CellNumber = "0433000003", CampusId = Campus("East London"),
                    CompanyName = "Academic Books Direct (demo)", ContactPerson = "Thabo Mokoena", Phone = "0433000003",
                    VatNumber = "4123456789", Address = "12 Oxford Street, East London"
                }, "https://localhost/Account/ResetPassword.aspx?token=");
                Ok(r, "supplier");
                SetPassword(r.Value);
                return ActorOf(r.Value);
            }

            private List<Actor> Tutors()
            {
                var list = new List<Actor>();
                var specs = new[]
                {
                    new { User = "tutor.dlamini", First = "Sipho", Last = "Dlamini", Qual = "BCom Accounting (Hons)", Years = 6, Rate = 80m, Campus = "Alice",
                          Subjects = new[] { ("Mathematics", "First Year"), ("Accounting", "Second Year") }, Verify = true },
                    new { User = "tutor.khumalo", First = "Ayanda", Last = "Khumalo", Qual = "BSc Computer Science", Years = 4, Rate = 85m, Campus = "East London",
                          Subjects = new[] { ("Information Systems", "Third Year"), ("Statistics", "Second Year") }, Verify = true },
                    new { User = "tutor.pending", First = "Lerato", Last = "Mahlangu", Qual = "BA Psychology", Years = 2, Rate = 70m, Campus = "Bisho",
                          Subjects = new[] { ("Psychology", "First Year") }, Verify = false }
                };
                foreach (var t in specs)
                {
                    var r = _s.Registration.RegisterTutor(new TutorRegistration
                    {
                        UserName = t.User, FirstName = t.First, LastName = t.Last, Email = t.User + "@studylink.local",
                        CellNumber = "0720000" + (list.Count + 101), CampusId = Campus(t.Campus), Qualification = t.Qual,
                        ExperienceYears = t.Years, HourlyRate = t.Rate, Gender = Gender.PreferNotToSay,
                        Bio = t.First + " has tutored " + t.Subjects[0].Item1 + " at Fort Hare for " + t.Years + " years.",
                        Subjects = t.Subjects.Select(x => new SubjectChoice { SubjectId = Subject(x.Item1), StudyLevelId = Level(x.Item2) }).ToList(),
                        Password = _pw, ConfirmPassword = _pw, AcceptedTerms = true
                    }, "seed-" + t.User);
                    Ok(r, t.User);
                    var actor = ActorOf(r.Value);
                    if (t.Verify)
                    {
                        var tutorId = _s.Db.Tutors.Single(x => x.UserId == r.Value).TutorId;
                        Ok(_s.Verification.Decide(_staff, tutorId, true, true, true, "ID and certified qualification seen at the Alice hub."), "verify " + t.User);
                        PublishWeek(actor, t.Campus);
                    }
                    list.Add(actor);
                }
                return list;
            }

            private void PublishWeek(Actor tutor, string campus)
            {
                var subjects = _s.Db.TutorSubjects.Where(x => x.Tutor.UserId == tutor.UserId).Select(x => x.TutorSubjectId).ToList();
                for (var d = 1; d <= 10; d++)
                {
                    var day = _now.Date.AddDays(d);
                    if (day.DayOfWeek == DayOfWeek.Sunday) continue;
                    var ts = subjects[d % subjects.Count];
                    foreach (var hour in new[] { 8, 10, 13, 15 })
                        _s.Availability.Add(tutor, ts, Campus(campus), day, TimeSpan.FromHours(hour), TimeSpan.FromHours(hour + 1), 0);
                }
            }

            private List<Actor> Students()
            {
                var specs = new[]
                {
                    ("student.thando", "Thando", "Mbeki", "S202400101", "Alice", "First Year"),
                    ("student.olona", "Olona", "Vane", "S202300456", "East London", "Second Year"),
                    ("student.aphiwe", "Aphiwe", "Ncube", "S202200789", "Bisho", "Third Year")
                };
                return specs.Select((x, i) =>
                {
                    var r = _s.Registration.RegisterStudent(new StudentRegistration
                    {
                        UserName = x.Item1, FirstName = x.Item2, LastName = x.Item3, Email = x.Item1 + "@studylink.local",
                        CellNumber = "0610000" + (100 + i), CampusId = Campus(x.Item5), StudentNumber = x.Item4,
                        StudyLevelId = Level(x.Item6), Faculty = "Management & Commerce", Password = _pw, ConfirmPassword = _pw,
                        AcceptedTerms = true, Gender = Gender.PreferNotToSay
                    }, "seed-" + x.Item1);
                    Ok(r, x.Item1);
                    return ActorOf(r.Value);
                }).ToList();
            }

            private List<int> Books(Actor supplier)
            {
                var supplierId = _s.Db.Suppliers.Single(x => x.UserId == supplier.UserId).SupplierId;
                var cat = _s.Db.Categories.ToDictionary(c => c.Name, c => c.CategoryId);
                var ids = new List<int>();
                foreach (var b in BookSpecs)
                {
                    var r = _s.Inventory.Save(_manager, new BookEdit
                    {
                        Isbn = b.Item1, Title = b.Item2, Authors = b.Item3, Edition = b.Item4, CategoryId = cat[b.Item5],
                        SubjectId = Subject(b.Item6), SalePrice = b.Item7, WeeklyRentalFee = b.Item8, LateFeePerDay = b.Item9,
                        ReorderLevel = b.Item10, PreferredSupplierId = supplierId, Publisher = "Demo Academic Press",
                        Description = "Prescribed text used across Fort Hare modules. Sample record created by the StudyLink demo seed."
                    }, allowMissingCover: true); // real covers are copied in from Images/covers at start-up
                    Ok(r, b.Item2);
                    ids.Add(r.Value);
                    var copies = ids.Count == 4 ? 1 : 3; // Calculus is deliberately scarce (low stock + waitlist demo)
                    Ok(_s.Inventory.AddCopies(_manager, r.Value, Campus("Alice"), copies, CopyCondition.New, null), "copies");
                    if (ids.Count % 2 == 0) Ok(_s.Inventory.AddCopies(_manager, r.Value, Campus("East London"), 2, CopyCondition.Good, null), "copies EL");
                    if (ids.Count % 3 == 0) Ok(_s.Inventory.AddCopies(_manager, r.Value, Campus("Bisho"), 1, CopyCondition.Good, null), "copies Bisho");
                }
                return ids;
            }

            private void Activity(List<Actor> tutors, List<Actor> students, List<int> books, Actor supplier)
            {
                var thando = students[0];
                var olona = students[1];
                var dlamini = tutors[0];
                var card = new CardDetails { Number = "4111 1111 1111 1111", Expiry = "12/30", Cvv = "123", Holder = "Demo Student" };

                // Thando: a confirmed session tomorrow and one waiting for the tutor.
                var slots = _s.Availability.OpenSlots(_s.Db.Tutors.Single(t => t.UserId == dlamini.UserId).TutorId);
                var first = _s.Bookings.Reserve(thando, slots[0].AvailabilityId, "Please help with journal entries.");
                Ok(first, "booking 1");
                Ok(_s.Payments.Pay(thando, Fee(bookingId: first.Value), card, Guid.NewGuid()), "pay booking 1");
                Ok(_s.Bookings.Respond(dlamini, first.Value, true, null), "confirm booking 1");
                var second = _s.Bookings.Reserve(olona, slots[3].AvailabilityId, "Exam preparation");
                Ok(second, "booking 2");
                Ok(_s.Payments.Pay(olona, Fee(bookingId: second.Value), card, Guid.NewGuid()), "pay booking 2");
                // An unpaid reservation shows on My Fees until it expires.
                Ok(_s.Bookings.Reserve(thando, slots[6].AvailabilityId, null), "booking 3");

                // Rentals: Thando has an active paid rental; Olona has one that is now overdue.
                var rental = _s.Rentals.Reserve(thando, books[0], Campus("Alice"));
                Ok(rental, "rental");
                Ok(_s.Payments.Pay(thando, Fee(rentalId: rental.Value), card, Guid.NewGuid()), "pay rental");
                var late = _s.Rentals.Reserve(olona, books[1], Campus("Alice"));
                Ok(late, "late rental");
                Ok(_s.Payments.Pay(olona, Fee(rentalId: late.Value), card, Guid.NewGuid()), "pay late rental");
                var overdue = _s.Db.Rentals.Find(late.Value);
                overdue.RentedAt = _now.AddDays(-12);
                overdue.DueDate = _now.Date.AddDays(-5);

                // A purchase.
                var sale = _s.Rentals.Buy(students[2], books[4], Campus("Alice"));
                Ok(sale, "sale");
                Ok(_s.Payments.Pay(students[2], Fee(saleId: sale.Value), card, Guid.NewGuid()), "pay sale");

                // History: a session delivered last week, with attendance and a review.
                PastSession(thando, dlamini);
                _s.Db.SaveChanges();

                // Supply chain: an open purchase order for the scarce title.
                var supplierId = _s.Db.Suppliers.Single(x => x.UserId == supplier.UserId).SupplierId;
                Ok(_s.SupplierOrders.Create(_manager, supplierId, _now.Date.AddDays(10), "Restock for the second semester.",
                    new List<NewOrderLine>
                    {
                        new NewOrderLine { BookId = books[3], CampusId = Campus("Alice"), Quantity = 6, UnitCost = 720m },
                        new NewOrderLine { BookId = books[2], CampusId = Campus("East London"), Quantity = 4, UnitCost = 650m }
                    }), "supplier order");

                // A support message and a pending password reset for the Staff queues.
                Ok(_s.Support.Submit(null, "Zoleka Mafu", "zoleka@example.com", Campus("Bisho"), "Are Law textbooks available at Bisho?",
                    "Hi, I am a first-year law student. Will the Introduction to SA Law book be available at the Bisho campus?", "seed"), "support");
                Ok(_s.PasswordResets.Request("student.aphiwe", "I forgot my password after the holidays.", "seed-reset"), "reset");

                _s.Maintenance.Run();
            }

            private IList<int> Fee(int? bookingId = null, int? rentalId = null, int? saleId = null) =>
                _s.Db.Fees.Where(f => f.Status == FeeStatus.Unpaid && ((bookingId.HasValue && f.BookingId == bookingId)
                                                                       || (rentalId.HasValue && f.RentalId == rentalId)
                                                                       || (saleId.HasValue && f.SaleId == saleId)))
                    .Select(f => f.FeeId).ToList();

            private void PastSession(Actor student, Actor tutor)
            {
                var tutorEntity = _s.Db.Tutors.Single(t => t.UserId == tutor.UserId);
                var ts = _s.Db.TutorSubjects.First(x => x.TutorId == tutorEntity.TutorId);
                var studentId = _s.Db.Students.Single(x => x.UserId == student.UserId).StudentId;
                var day = SchedulePolicy.WeekStart(_now.Date).AddDays(-5);
                var slot = _s.Db.TutorAvailability.Add(new TutorAvailability
                {
                    TutorSubjectId = ts.TutorSubjectId, CampusId = _s.Db.Users.Find(tutor.UserId).CampusId, AvailabilityDate = day,
                    StartTime = TimeSpan.FromHours(10), EndTime = TimeSpan.FromHours(11), IsBooked = true, CreatedAt = day.AddDays(-3)
                });
                var booking = _s.Db.Bookings.Add(new Booking
                {
                    Reference = "BK-DEMO-001", StudentId = studentId, TutorId = tutorEntity.TutorId, TutorSubjectId = ts.TutorSubjectId,
                    Availability = slot, CampusId = slot.CampusId, SessionDate = day, StartTime = slot.StartTime, EndTime = slot.EndTime,
                    Fee = tutorEntity.HourlyRate, Status = BookingStatus.Completed, CreatedAt = day.AddDays(-2), PaidAt = day.AddDays(-2),
                    TutorRespondedAt = day.AddDays(-2), CompletedAt = day.AddHours(11)
                });
                var fee = _s.Db.Fees.Add(new Fee
                {
                    StudentId = studentId, Type = FeeType.TutoringFee, Amount = booking.Fee, Booking = booking,
                    Description = "Tutoring – " + _s.Db.Subjects.Find(ts.SubjectId).Name + " with " + _s.Db.Users.Find(tutor.UserId).FullName,
                    Status = FeeStatus.Paid, CreatedAt = day.AddDays(-2), SettledAt = day.AddDays(-2)
                });
                var payment = _s.Db.Payments.Add(new Payment
                {
                    Reference = "SL-PAY-DEMO-0001", StudentId = studentId, Type = PaymentType.Charge, Status = PaymentStatus.Completed,
                    Amount = booking.Fee, CardBrand = "Visa", CardLast4 = "1111", GatewayReference = "SIM-DEMO", CreatedAt = day.AddDays(-2),
                    CompletedAt = day.AddDays(-2)
                });
                _s.Db.PaymentAllocations.Add(new PaymentAllocation { Payment = payment, Fee = fee, Amount = booking.Fee });
                _s.Db.TutoringSessions.Add(new TutoringSession
                {
                    Booking = booking, SessionDate = day, Attendance = AttendanceStatus.Attended, HoursDelivered = 1m,
                    TutorNotes = "Worked through adjusting entries and trial balance errors.", CreatedAt = day.AddHours(11)
                });
                _s.Db.Reviews.Add(new Review { Booking = booking, Rating = 5, Comment = "Sipho explains adjusting entries really clearly.", CreatedAt = day.AddDays(1) });
            }
        }
    }
}