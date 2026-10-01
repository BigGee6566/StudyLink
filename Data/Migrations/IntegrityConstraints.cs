using System;

namespace StudyLink.Data.Migrations
{
    /// <summary>
    /// Database-level backstops EF6 cannot express: filtered unique indexes and CHECK constraints.
    /// Services enforce every rule first; these make the rules hold even if two requests race or
    /// someone edits data by hand. Called at the end of InitialCreate.Up / start of Down.
    /// Enum numbers are those in Entities\Enums.cs.
    /// </summary>
    internal static class IntegrityConstraints
    {
        public static void Up(Action<string> sql)
        {
            // Double-booking backstop: only one ACTIVE booking (AwaitingPayment 1, AwaitingTutor 2,
            // Confirmed 3) may hold a slot. The SERIALIZABLE reserve path is the first line of defence.
            sql("CREATE UNIQUE INDEX UX_Bookings_ActiveSlot ON dbo.Bookings (AvailabilityId) WHERE Status IN (1, 2, 3);");
            sql("CREATE INDEX IX_Bookings_Tutor_Date ON dbo.Bookings (TutorId, SessionDate, Status);");
            sql("CREATE INDEX IX_Bookings_Student_Date ON dbo.Bookings (StudentId, SessionDate, Status);");

            // Stock backstop: one copy cannot be in two live rentals (PendingPayment 1, Active 2) or two live sales.
            sql("CREATE UNIQUE INDEX UX_Rentals_ActiveCopy ON dbo.Rentals (CopyId) WHERE Status IN (1, 2);");
            sql("CREATE UNIQUE INDEX UX_Sales_ActiveCopy ON dbo.Sales (CopyId) WHERE Status IN (1, 2);");
            sql("CREATE INDEX IX_BookCopies_Book_Campus_Status ON dbo.BookCopies (BookId, CampusId, Status);");
            sql("CREATE INDEX IX_Rentals_Status_Due ON dbo.Rentals (Status, DueDate);");

            // Ported from TutorConnect: one unresolved postponement proposal per booking.
            sql("CREATE UNIQUE INDEX UX_Postponement_OnePending ON dbo.PostponementRequests (BookingId) WHERE Status = 1;");

            // A double-submitted checkout must not charge twice; notifications from the sweep are sent once.
            sql("CREATE UNIQUE INDEX UX_Payments_CheckoutToken ON dbo.Payments (CheckoutToken) WHERE CheckoutToken IS NOT NULL;");
            sql("CREATE UNIQUE INDEX UX_Notifications_DedupKey ON dbo.Notifications (DedupKey) WHERE DedupKey IS NOT NULL;");
            sql("CREATE INDEX IX_Fees_Student_Status ON dbo.Fees (StudentId, Status);");

            // CHECK constraints.
            sql("ALTER TABLE dbo.TutorAvailability ADD CONSTRAINT CK_TutorAvailability_Times CHECK (EndTime > StartTime);");
            sql("ALTER TABLE dbo.Bookings ADD CONSTRAINT CK_Bookings_Times CHECK (EndTime > StartTime);");
            sql("ALTER TABLE dbo.Bookings ADD CONSTRAINT CK_Bookings_Fee CHECK (Fee >= 0);");
            sql("ALTER TABLE dbo.Bookings ADD CONSTRAINT CK_Bookings_CancelConsistency CHECK ("
                + "(Status = 5 AND CancelledAt IS NOT NULL AND CancelReasonCategory IS NOT NULL) OR "
                + "(Status <> 5 AND CancelledAt IS NULL AND CancelledByUserId IS NULL AND CancelReasonCategory IS NULL));");
            sql("ALTER TABLE dbo.PostponementRequests ADD CONSTRAINT CK_Postponement_Times CHECK (ProposedEnd > ProposedStart);");
            sql("ALTER TABLE dbo.PostponementRequests ADD CONSTRAINT CK_Postponement_Responded CHECK ("
                + "(Status = 1 AND RespondedAt IS NULL) OR (Status <> 1 AND RespondedAt IS NOT NULL));");
            sql("ALTER TABLE dbo.Reviews ADD CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5);");
            sql("ALTER TABLE dbo.Tutors ADD CONSTRAINT CK_Tutors_Values CHECK (HourlyRate >= 0 AND ExperienceYears BETWEEN 0 AND 70);");
            sql("ALTER TABLE dbo.Books ADD CONSTRAINT CK_Books_Prices CHECK (SalePrice >= 0 AND WeeklyRentalFee >= 0 AND LateFeePerDay >= 0 AND ReorderLevel >= 0);");
            sql("ALTER TABLE dbo.Rentals ADD CONSTRAINT CK_Rentals_Values CHECK (Fee >= 0 AND RenewalCount >= 0);");
            sql("ALTER TABLE dbo.Fines ADD CONSTRAINT CK_Fines_Amount CHECK (Amount >= 0 AND DaysLate >= 0);");
            sql("ALTER TABLE dbo.Fees ADD CONSTRAINT CK_Fees_Amount CHECK (Amount >= 0);");
            sql("ALTER TABLE dbo.Fees ADD CONSTRAINT CK_Fees_ExactlyOneSource CHECK ("
                + "(CASE WHEN BookingId IS NULL THEN 0 ELSE 1 END + CASE WHEN RentalId IS NULL THEN 0 ELSE 1 END"
                + " + CASE WHEN RentalRenewalId IS NULL THEN 0 ELSE 1 END + CASE WHEN SaleId IS NULL THEN 0 ELSE 1 END"
                + " + CASE WHEN FineId IS NULL THEN 0 ELSE 1 END) = 1);");
            sql("ALTER TABLE dbo.Payments ADD CONSTRAINT CK_Payments_Amount CHECK (Amount > 0);");
            sql("ALTER TABLE dbo.PaymentAllocations ADD CONSTRAINT CK_PaymentAllocations_Amount CHECK (Amount > 0);");
            sql("ALTER TABLE dbo.SupplierOrderLines ADD CONSTRAINT CK_OrderLines_Qty CHECK (QuantityOrdered > 0 AND QuantityDelivered >= 0 AND QuantityDelivered <= QuantityOrdered AND UnitCost >= 0);");
            sql("ALTER TABLE dbo.DeliveryLines ADD CONSTRAINT CK_DeliveryLines_Qty CHECK (Quantity > 0 AND (QuantityAccepted IS NULL OR QuantityAccepted BETWEEN 0 AND Quantity));");
            sql("ALTER TABLE dbo.TimesheetEntries ADD CONSTRAINT CK_TimesheetEntries_Hours CHECK (Hours > 0 AND Hours <= 24);");
            sql("ALTER TABLE dbo.SupplierInvoices ADD CONSTRAINT CK_SupplierInvoices_Amount CHECK (Amount >= 0);");
        }

        public static void Down(Action<string> sql)
        {
            foreach (var name in new[]
            {
                "UX_Bookings_ActiveSlot ON dbo.Bookings", "IX_Bookings_Tutor_Date ON dbo.Bookings",
                "IX_Bookings_Student_Date ON dbo.Bookings", "UX_Rentals_ActiveCopy ON dbo.Rentals",
                "UX_Sales_ActiveCopy ON dbo.Sales", "IX_BookCopies_Book_Campus_Status ON dbo.BookCopies",
                "IX_Rentals_Status_Due ON dbo.Rentals", "UX_Postponement_OnePending ON dbo.PostponementRequests",
                "UX_Payments_CheckoutToken ON dbo.Payments", "UX_Notifications_DedupKey ON dbo.Notifications",
                "IX_Fees_Student_Status ON dbo.Fees"
            })
            {
                sql("DROP INDEX IF EXISTS " + name + ";");
            }
            // CHECK constraints disappear with their tables when InitialCreate.Down drops them.
        }
    }
}
