using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using StudyLink.Data.Configurations;
using StudyLink.Data.Entities;

namespace StudyLink.Data
{
    public class StudyLinkDbContext : DbContext
    {
        /// <summary>
        /// Uses the "StudyLink" connection string from Web.config / App.config. Not "name=StudyLink":
        /// the looser form lets design-time tooling (ef6.exe) supply its own connection.
        /// </summary>
        public StudyLinkDbContext() : this("StudyLink") { }

        public StudyLinkDbContext(string nameOrConnectionString) : base(nameOrConnectionString) { }

        // People and security
        public DbSet<Campus> Campuses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<StaffMember> Staff { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Manager> Managers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<TutorVerification> TutorVerifications { get; set; }
        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<ProfileUpdateRequest> ProfileUpdateRequests { get; set; }
        public DbSet<SupportMessage> SupportMessages { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        // Tutoring
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<StudyLevel> StudyLevels { get; set; }
        public DbSet<StudentSubject> StudentSubjects { get; set; }
        public DbSet<TutorSubject> TutorSubjects { get; set; }
        public DbSet<TutorAvailability> TutorAvailability { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<TutoringSession> TutoringSessions { get; set; }
        public DbSet<PostponementRequest> PostponementRequests { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Timesheet> Timesheets { get; set; }
        public DbSet<TimesheetEntry> TimesheetEntries { get; set; }

        // Books
        public DbSet<Category> Categories { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<BookCopy> BookCopies { get; set; }
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<RentalRenewal> RentalRenewals { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<Fine> Fines { get; set; }
        public DbSet<BookWaitlist> BookWaitlists { get; set; }

        // Money
        public DbSet<Fee> Fees { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentAllocation> PaymentAllocations { get; set; }

        // Supply
        public DbSet<SupplierOrder> SupplierOrders { get; set; }
        public DbSet<SupplierOrderLine> SupplierOrderLines { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<DeliveryLine> DeliveryLines { get; set; }
        public DbSet<SupplierInvoice> SupplierInvoices { get; set; }

        // Operations and reporting
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ReportRun> ReportRuns { get; set; }
        public DbSet<DailyMetric> DailyMetrics { get; set; }
        public DbSet<TutorStat> TutorStats { get; set; }
        public DbSet<BackupLog> BackupLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // No cascading deletes anywhere: history (payments, audit, bookings) must never vanish
            // because a parent row was removed, and SQL Server rejects multiple cascade paths anyway.
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            modelBuilder.Properties<decimal>().Configure(c => c.HasPrecision(12, 2));
            modelBuilder.Properties<TimeSpan>().Configure(c => c.HasPrecision(0));
            modelBuilder.Properties<DateTime>()
                .Where(p => !p.GetCustomAttributes(typeof(ColumnAttribute), false).Any())
                .Configure(c => c.HasColumnType("datetime2").HasPrecision(0));

            PeopleConfiguration.Apply(modelBuilder);
            TutoringConfiguration.Apply(modelBuilder);
            BooksConfiguration.Apply(modelBuilder);
            MoneyConfiguration.Apply(modelBuilder);
            SupplyConfiguration.Apply(modelBuilder);
        }
    }
}
