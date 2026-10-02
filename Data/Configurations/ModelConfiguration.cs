using System.Data.Entity;
using StudyLink.Data.Entities;

namespace StudyLink.Data.Configurations
{
    // Only relationships EF6 cannot infer by convention are configured here: foreign keys whose
    // names do not follow <Navigation><PrincipalKey>, and types with two navigations to the same
    // principal. Everything else (lengths, unique indexes) is declared on the entities.

    internal static class PeopleConfiguration
    {
        public static void Apply(DbModelBuilder mb)
        {
            mb.Entity<User>()
                .HasOptional(u => u.CreatedBy).WithMany().HasForeignKey(u => u.CreatedByUserId);

            // Shared primary key: UserPhotos.UserId is both the key and the foreign key to Users.
            mb.Entity<UserPhoto>().HasKey(p => p.UserId);
            mb.Entity<UserPhoto>().HasRequired(p => p.User).WithOptional();

            mb.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
            mb.Entity<UserRole>()
                .HasRequired(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
            mb.Entity<UserRole>()
                .HasRequired(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
            mb.Entity<UserRole>()
                .HasOptional(ur => ur.AssignedBy).WithMany().HasForeignKey(ur => ur.AssignedByUserId);

            // These three have two navigations to User (the requester and the staff member who
            // handled it). Without explicit keys EF6 invents a shadow User_UserId column and leaves
            // UserId unused - caught by the password-reset integration test.
            mb.Entity<PasswordResetRequest>()
                .HasOptional(r => r.User).WithMany().HasForeignKey(r => r.UserId);
            mb.Entity<PasswordResetRequest>()
                .HasOptional(r => r.HandledBy).WithMany().HasForeignKey(r => r.HandledByUserId);
            mb.Entity<ProfileUpdateRequest>()
                .HasRequired(r => r.User).WithMany().HasForeignKey(r => r.UserId);
            mb.Entity<ProfileUpdateRequest>()
                .HasOptional(r => r.HandledBy).WithMany().HasForeignKey(r => r.HandledByUserId);
            mb.Entity<SupportMessage>()
                .HasOptional(m => m.User).WithMany().HasForeignKey(m => m.UserId);
            mb.Entity<SupportMessage>()
                .HasOptional(m => m.HandledBy).WithMany().HasForeignKey(m => m.HandledByUserId);

            mb.Entity<TutorVerification>()
                .HasRequired(v => v.Tutor).WithMany(t => t.Verifications).HasForeignKey(v => v.TutorId);
            mb.Entity<TutorVerification>()
                .HasOptional(v => v.ReviewedByStaff).WithMany().HasForeignKey(v => v.ReviewedByStaffId);
        }
    }

    internal static class TutoringConfiguration
    {
        public static void Apply(DbModelBuilder mb)
        {
            mb.Entity<TutorSubject>()
                .HasRequired(ts => ts.Tutor).WithMany(t => t.Subjects).HasForeignKey(ts => ts.TutorId);

            mb.Entity<Booking>()
                .HasRequired(b => b.Availability).WithMany().HasForeignKey(b => b.AvailabilityId);

            mb.Entity<TimesheetEntry>()
                .HasRequired(e => e.Timesheet).WithMany(t => t.Entries).HasForeignKey(e => e.TimesheetId);

            mb.Entity<Review>().Property(r => r.Rating).HasColumnType("tinyint");
        }
    }

    internal static class BooksConfiguration
    {
        public static void Apply(DbModelBuilder mb)
        {
            mb.Entity<Book>()
                .HasOptional(b => b.PreferredSupplier).WithMany().HasForeignKey(b => b.PreferredSupplierId);
            mb.Entity<Book>()
                .HasOptional(b => b.CreatedByManager).WithMany().HasForeignKey(b => b.CreatedByManagerId);
            mb.Entity<Book>()
                .HasOptional(b => b.UpdatedByManager).WithMany().HasForeignKey(b => b.UpdatedByManagerId);
            mb.Entity<Book>().Property(b => b.CoverImage).HasColumnType("varbinary(max)");

            mb.Entity<BookCopy>()
                .HasRequired(c => c.Book).WithMany(b => b.Copies).HasForeignKey(c => c.BookId);

            mb.Entity<RentalRenewal>()
                .HasRequired(r => r.Rental).WithMany(r => r.Renewals).HasForeignKey(r => r.RentalId);
            mb.Entity<Fine>()
                .HasRequired(f => f.Rental).WithMany(r => r.Fines).HasForeignKey(f => f.RentalId);
        }
    }

    internal static class MoneyConfiguration
    {
        public static void Apply(DbModelBuilder mb)
        {
            mb.Entity<Fee>()
                .HasOptional(f => f.RentalRenewal).WithMany().HasForeignKey(f => f.RentalRenewalId);

            mb.Entity<PaymentAllocation>()
                .HasRequired(a => a.Payment).WithMany(p => p.Allocations).HasForeignKey(a => a.PaymentId);
            mb.Entity<PaymentAllocation>()
                .HasRequired(a => a.Fee).WithMany(f => f.Allocations).HasForeignKey(a => a.FeeId);

            mb.Entity<Payment>()
                .HasOptional(p => p.RefundOf).WithMany().HasForeignKey(p => p.RefundOfPaymentId);
        }
    }

    internal static class SupplyConfiguration
    {
        public static void Apply(DbModelBuilder mb)
        {
            mb.Entity<SupplierOrder>()
                .HasRequired(o => o.CreatedByManager).WithMany().HasForeignKey(o => o.CreatedByManagerId);

            mb.Entity<SupplierOrderLine>()
                .HasRequired(l => l.Order).WithMany(o => o.Lines).HasForeignKey(l => l.OrderId);

            mb.Entity<Delivery>()
                .HasRequired(d => d.Order).WithMany(o => o.Deliveries).HasForeignKey(d => d.OrderId);

            mb.Entity<DeliveryLine>()
                .HasRequired(l => l.Delivery).WithMany(d => d.Lines).HasForeignKey(l => l.DeliveryId);
            mb.Entity<DeliveryLine>()
                .HasRequired(l => l.OrderLine).WithMany().HasForeignKey(l => l.OrderLineId);

            mb.Entity<BookCopy>()
                .HasOptional(c => c.DeliveryLine).WithMany().HasForeignKey(c => c.DeliveryLineId);
        }
    }
}
