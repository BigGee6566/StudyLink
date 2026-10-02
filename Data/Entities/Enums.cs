namespace StudyLink.Data.Entities
{
    // Enums are stored as INT. Explicit values keep existing rows stable if a member is added,
    // and the numbers are documented in database\StudyLink.sql for anyone querying by hand.

    public enum Gender { Male = 1, Female = 2, Other = 3, PreferNotToSay = 4 }

    /// <summary>A tutor cannot publish availability or be booked until Verified (brief §7).</summary>
    public enum TutorVerificationStatus { PendingVerification = 1, Verified = 2, Rejected = 3, Suspended = 4 }

    public enum VerificationDecision { Pending = 1, Approved = 2, Rejected = 3 }

    /// <summary>
    /// D-4: AwaitingPayment holds the slot until the hold expires; paying moves it to AwaitingTutor;
    /// the tutor then confirms or declines. The first three states are the "active" ones that the
    /// filtered unique index on AvailabilityId covers.
    /// </summary>
    public enum BookingStatus
    {
        AwaitingPayment = 1,
        AwaitingTutor = 2,
        Confirmed = 3,
        Completed = 4,
        Cancelled = 5,
        Declined = 6,
        Expired = 7
    }

    public enum AttendanceStatus { NotRecorded = 0, Attended = 1, StudentAbsent = 2, TutorAbsent = 3 }

    /// <summary>Ported from TutorConnect ScheduleChangeReason (cancellation and postponement).</summary>
    public enum ScheduleChangeReason
    {
        TimetableClash = 1,
        Medical = 2,
        ExamPreparation = 3,
        AssignmentDeadline = 4,
        Other = 5
    }

    public enum PostponementStatus { PendingTutorReview = 1, Approved = 2, Declined = 3, Cancelled = 4 }

    public enum TimesheetStatus { Draft = 1, Submitted = 2, Approved = 3, Rejected = 4 }

    public enum CopyCondition { New = 1, Good = 2, Fair = 3, Poor = 4, Damaged = 5 }

    /// <summary>Stock is never stored as a number: it is always counted from copy statuses.</summary>
    public enum CopyStatus
    {
        Available = 1,
        Reserved = 2,
        Rented = 3,
        Sold = 4,
        Lost = 5,
        Damaged = 6,
        WrittenOff = 7
    }

    public enum RentalStatus { PendingPayment = 1, Active = 2, Returned = 3, Lost = 4, Cancelled = 5 }

    public enum SaleStatus { PendingPayment = 1, Completed = 2, Cancelled = 3 }

    public enum FineType { Overdue = 1, Damage = 2, Lost = 3 }

    public enum FineStatus { Outstanding = 1, Paid = 2, Waived = 3 }

    /// <summary>Each billable item a student can owe appears once in the Fees ledger (SC14 My Fees).</summary>
    public enum FeeType
    {
        TutoringFee = 1,
        RentalFee = 2,
        RenewalFee = 3,
        BookPurchase = 4,
        OverdueFine = 5,
        DamageFine = 6,
        LostBookFine = 7
    }

    public enum FeeStatus { Unpaid = 1, Paid = 2, Cancelled = 3, Refunded = 4, Waived = 5 }

    public enum PaymentType { Charge = 1, Refund = 2 }

    public enum PaymentMethod { Card = 1 }

    public enum PaymentStatus { Pending = 1, Completed = 2, Failed = 3 }

    public enum SupplierOrderStatus
    {
        Placed = 1,
        Acknowledged = 2,
        PartiallyDelivered = 3,
        Delivered = 4,
        Cancelled = 5
    }

    public enum DeliveryStatus { Dispatched = 1, Received = 2, Rejected = 3 }

    public enum InvoiceStatus { Submitted = 1, Approved = 2, Paid = 3, Rejected = 4 }

    /// <summary>Password-reset and profile-update requests share one lifecycle.</summary>
    public enum RequestStatus { Pending = 1, Approved = 2, Rejected = 3, Completed = 4 }

    public enum SupportStatus { Open = 1, InProgress = 2, Resolved = 3 }

    public enum BackupStatus { Running = 1, Succeeded = 2, Failed = 3 }

    public enum NotificationType
    {
        // Ported from TutorConnect
        BookingCompleted = 5,
        BookingCancelled = 6,
        ReviewReceived = 7,
        AccountDeactivated = 9,
        BookingPostponeRequested = 10,
        BookingPostponeApproved = 11,
        BookingPostponeDeclined = 12,
        BookingPostponeWithdrawn = 13,
        BookingRescheduled = 14,

        // StudyLink tutoring
        BookingCreated = 20,
        BookingConfirmed = 21,
        BookingDeclined = 22,
        BookingExpired = 23,
        TutorRegistrationSubmitted = 24,
        TutorVerificationApproved = 25,
        TutorVerificationRejected = 26,
        TimesheetSubmitted = 27,
        TimesheetReviewed = 28,

        // Books and money
        RentalConfirmed = 40,
        RentalDueSoon = 41,
        RentalOverdue = 42,
        RentalReturned = 43,
        FineCreated = 44,
        PaymentRecorded = 45,
        RefundIssued = 46,
        WaitlistAvailable = 47,
        LowStock = 48,

        // Supply chain
        SupplierOrderCreated = 60,
        SupplierDeliveryDispatched = 61,
        SupplierDeliveryReceived = 62,
        InvoiceSubmitted = 63,
        InvoiceStatusChanged = 64,

        // Accounts and administration
        PasswordResetRequested = 80,
        PasswordResetApproved = 81,
        ProfileUpdateRequested = 82,
        ProfileUpdateReviewed = 83,
        AccountCreated = 84,
        SupportMessageReceived = 85,
        Administrative = 99
    }
}
