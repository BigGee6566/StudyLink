using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StudyLink.Data;
using StudyLink.Data.Entities;
using StudyLink.Data.Seed;
using StudyLink.Services;
using StudyLink.Services.DTOs;
using StudyLink.Services.Implementations;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Interfaces;
using StudyLink.Services.Security;

namespace StudyLink.Tests
{
    /// <summary>
    /// Integration tests run against a real, throwaway SQL Server database (StudyLink_Test) built
    /// from the same EF6 migrations as production, so the filtered unique indexes, CHECK
    /// constraints and UPDLOCK/HOLDLOCK paths are exercised for real. Override the server with the
    /// STUDYLINK_TEST_SERVER environment variable (default .\SQLEXPRESS).
    /// </summary>
    [TestClass]
    public static class TestDatabase
    {
        public static string ConnectionString { get; private set; }

        [AssemblyInitialize]
        public static void Create(TestContext _)
        {
            var server = Environment.GetEnvironmentVariable("STUDYLINK_TEST_SERVER") ?? @".\SQLEXPRESS";
            ConnectionString = $"Data Source={server};Initial Catalog=StudyLink_Test;Integrated Security=True;MultipleActiveResultSets=False";
            Database.SetInitializer<StudyLinkDbContext>(null);

            Drop(server);
            var config = new StudyLink.Data.Migrations.Configuration
            {
                TargetDatabase = new System.Data.Entity.Infrastructure.DbConnectionInfo(ConnectionString, "System.Data.SqlClient")
            };
            new DbMigrator(config).Update(); // schema + integrity constraints + LookupData seed
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            SqlConnection.ClearAllPools();
            if (Environment.GetEnvironmentVariable("STUDYLINK_KEEP_TEST_DB") != "1")
                Drop(new SqlConnectionStringBuilder(ConnectionString).DataSource);
        }

        private static void Drop(string server)
        {
            SqlConnection.ClearAllPools();
            using (var c = new SqlConnection($"Data Source={server};Initial Catalog=master;Integrated Security=True"))
            {
                c.Open();
                new SqlCommand("IF DB_ID('StudyLink_Test') IS NOT NULL BEGIN ALTER DATABASE StudyLink_Test SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE StudyLink_Test; END", c)
                    .ExecuteNonQuery();
            }
        }
    }

    /// <summary>A clock tests can move; everything time-based reads from it.</summary>
    public sealed class FakeClock : IClock
    {
        public FakeClock(DateTime now) { Now = now; }
        public DateTime Now { get; set; }
        public DateTime Today => Now.Date;
        public void Advance(TimeSpan by) => Now = Now.Add(by);
    }

    public sealed class CapturingEmail : IEmailSender
    {
        public ConcurrentBag<Tuple<string, string, string>> Sent { get; } = new ConcurrentBag<Tuple<string, string, string>>();
        public void Send(string to, string subject, string htmlBody) => Sent.Add(Tuple.Create(to, subject, htmlBody));
    }

    /// <summary>
    /// Builds test data through the real services where the rule under test is involved, and
    /// directly where it is just setup. Every name carries a random suffix so tests never collide.
    /// </summary>
    public sealed class World : IDisposable
    {
        public const string Password = "Passw0rd!Test";
        public const string GoodCard = "4111 1111 1111 1111";

        public World(DateTime? now = null)
        {
            Clock = new FakeClock(now ?? new DateTime(2026, 10, 5, 9, 0, 0)); // a Monday morning
            Email = new CapturingEmail();
            Files = new DiskFileStore(Path.Combine(Path.GetTempPath(), "StudyLinkTests"));
            S = NewServices();
        }

        public FakeClock Clock { get; }
        public CapturingEmail Email { get; }
        public IFileStore Files { get; }
        public StudyLinkServices S { get; private set; }

        /// <summary>A second, independent unit of work - what a concurrent request would have.</summary>
        public StudyLinkServices NewServices() =>
            new StudyLinkServices(new StudyLinkDbContext(TestDatabase.ConnectionString), Clock, null, Email, Files);

        /// <summary>Drops cached entities so assertions read what is really in the database.</summary>
        public void Refresh()
        {
            S.Dispose();
            S = NewServices();
        }

        public static string Suffix() => ReferenceGenerator.RandomCode(6);

        public int Campus(string name = "Alice") => S.Db.Campuses.Single(c => c.Name == name).CampusId;
        public int SubjectId(string name = "Mathematics") => S.Db.Subjects.Single(s => s.Name == name).SubjectId;
        public int LevelId(string name = "First Year") => S.Db.StudyLevels.Single(l => l.Name == name).StudyLevelId;

        public Actor ActorFor(int userId)
        {
            var user = S.Db.Users.Find(userId);
            return new Actor(userId, user.UserName, S.Auth.RolesOf(userId), "127.0.0.1");
        }

        public Actor NewStudent()
        {
            var x = Suffix();
            var result = S.Registration.RegisterStudent(new StudentRegistration
            {
                UserName = "stu_" + x, FirstName = "Thando", LastName = "Student" + x, Email = "stu_" + x + "@test.local",
                CellNumber = "0821234567", CampusId = Campus(), StudentNumber = "S" + x, Password = Password, ConfirmPassword = Password,
                AcceptedTerms = true
            }, "test-ip-" + x); // unique key: the registration limiter is process-wide
            Assert.IsTrue(result.Succeeded, result.Error);
            return ActorFor(result.Value);
        }

        /// <summary>Registers a tutor (pending) and, unless told otherwise, verifies them as a staff member would.</summary>
        public Actor NewTutor(decimal rate = 100m, bool verify = true)
        {
            var x = Suffix();
            var result = S.Registration.RegisterTutor(new TutorRegistration
            {
                UserName = "tut_" + x, FirstName = "Sipho", LastName = "Tutor" + x, Email = "tut_" + x + "@test.local",
                CellNumber = "0831234567", CampusId = Campus(), Qualification = "BSc Mathematics", ExperienceYears = 3, HourlyRate = rate,
                Subjects = new List<SubjectChoice> { new SubjectChoice { SubjectId = SubjectId(), StudyLevelId = LevelId() } },
                Password = Password, ConfirmPassword = Password, AcceptedTerms = true
            }, "test-ip-" + x);
            Assert.IsTrue(result.Succeeded, result.Error);
            var tutor = ActorFor(result.Value);
            if (verify)
            {
                var tutorId = S.Db.Tutors.Single(t => t.UserId == tutor.UserId).TutorId;
                var decision = S.Verification.Decide(NewStaff(), tutorId, true, true, true, "Documents checked");
                Assert.IsTrue(decision.Succeeded, decision.Error);
            }
            return tutor;
        }

        public Actor NewAccount(string role, Actor creator = null)
        {
            var x = Suffix();
            var request = new CreateAccountRequest
            {
                Role = role, UserName = role.ToLowerInvariant() + "_" + x, FirstName = role, LastName = "User" + x,
                Email = role.ToLowerInvariant() + "_" + x + "@test.local", CellNumber = "0841234567", CampusId = Campus(),
                CompanyName = "Books Direct " + x, ContactPerson = "Mr Supplier", Phone = "0431234567", StudentNumber = "C" + x
            };
            var result = S.Users.CreateAccount(creator ?? Admin(), request, "https://test/reset?token=");
            Assert.IsTrue(result.Succeeded, result.Error);
            return ActorFor(result.Value);
        }

        public Actor NewStaff() => NewAccount(RoleNames.Staff);
        public Actor NewManager() => NewAccount(RoleNames.Manager);
        public Actor NewSupplier() => NewAccount(RoleNames.Supplier);

        private Actor _admin;

        /// <summary>A System Admin created directly (the first-run Setup page is tested separately).</summary>
        public Actor Admin()
        {
            if (_admin != null) return _admin;
            var x = Suffix();
            var hashed = PasswordHasher.Create(Password);
            var user = new User
            {
                UserName = "admin_" + x, FirstName = "Ada", LastName = "Admin", Email = "admin_" + x + "@test.local", CellNumber = "0851234567",
                CampusId = Campus(), PasswordHash = hashed.Hash, PasswordSalt = hashed.Salt, HashIterations = hashed.Iterations,
                IsActive = true, CreatedAt = Clock.Now
            };
            user.UserRoles.Add(new UserRole { RoleId = S.Db.Roles.Single(r => r.Name == RoleNames.SystemAdmin).RoleId, AssignedAt = Clock.Now });
            S.Db.Users.Add(user);
            S.Db.Admins.Add(new Admin { User = user });
            S.Db.SaveChanges();
            return _admin = ActorFor(user.UserId);
        }

        public int TutorSubjectOf(Actor tutor) => S.Db.TutorSubjects.First(ts => ts.Tutor.UserId == tutor.UserId).TutorSubjectId;

        /// <summary>Publishes one slot through AvailabilityService (so BR-06/07/20 apply) and returns its id.</summary>
        public int Slot(Actor tutor, DateTime date, int startHour, int endHour)
        {
            var start = TimeSpan.FromHours(startHour);
            var result = S.Availability.Add(tutor, TutorSubjectOf(tutor), Campus(), date, start, TimeSpan.FromHours(endHour), 0);
            Assert.IsTrue(result.Succeeded, result.Error);
            var day = date.Date;
            return S.Db.TutorAvailability.Where(a => a.TutorSubject.Tutor.UserId == tutor.UserId && a.AvailabilityDate == day
                                                     && a.StartTime == start)
                .Select(a => a.AvailabilityId).Single();
        }

        public int Book(Actor manager, int copiesAtAlice, decimal salePrice = 500m, decimal weekly = 50m, decimal lateFee = 10m, int reorder = 1)
        {
            var x = Suffix();
            var isbn = Isbn13("978" + NextDigits(9));
            var saved = S.Inventory.Save(manager, new BookEdit
            {
                Isbn = isbn, Title = "Financial Accounting " + x, Authors = "Test Author", CategoryId = S.Db.Categories.First().CategoryId,
                SalePrice = salePrice, WeeklyRentalFee = weekly, LateFeePerDay = lateFee, ReorderLevel = reorder
            });
            Assert.IsTrue(saved.Succeeded, saved.Error);
            if (copiesAtAlice > 0)
                Assert.IsTrue(S.Inventory.AddCopies(manager, saved.Value, Campus(), copiesAtAlice, CopyCondition.New, null).Succeeded);
            return saved.Value;
        }

        private static readonly Random Rng = new Random();

        private static string NextDigits(int count)
        {
            lock (Rng)
                return string.Concat(Enumerable.Range(0, count).Select(_ => Rng.Next(10)));
        }

        /// <summary>Completes a 12-digit prefix into a valid ISBN-13.</summary>
        public static string Isbn13(string twelve)
        {
            twelve = twelve.Substring(0, 12);
            var sum = 0;
            for (var i = 0; i < 12; i++) sum += (twelve[i] - '0') * (i % 2 == 0 ? 1 : 3);
            return twelve + ((10 - sum % 10) % 10);
        }

        public static CardDetails Card(string number = GoodCard) =>
            new CardDetails { Number = number, Expiry = "12/30", Cvv = "123", Holder = "Thando Student" };

        public int FeeFor(int? bookingId = null, int? rentalId = null, int? saleId = null) =>
            S.Db.Fees.Where(f => (bookingId.HasValue && f.BookingId == bookingId) || (rentalId.HasValue && f.RentalId == rentalId)
                                 || (saleId.HasValue && f.SaleId == saleId))
                .OrderByDescending(f => f.FeeId).Select(f => f.FeeId).First();

        public void Dispose() => S.Dispose();
    }
}
