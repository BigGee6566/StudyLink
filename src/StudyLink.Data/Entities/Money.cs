using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyLink.Data.Entities
{
    /// <summary>
    /// The "My Fees" ledger (SC14). Every billable item a student owes is one row, linked to
    /// exactly one source (a CHECK constraint in the migration enforces "exactly one").
    /// Booking → Fee → PaymentAllocation → Payment is how Booking → Payments is realised.
    /// </summary>
    public class Fee
    {
        public int FeeId { get; set; }

        public int StudentId { get; set; }
        public virtual Student Student { get; set; }

        public FeeType Type { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; }

        public decimal Amount { get; set; }
        public FeeStatus Status { get; set; } = FeeStatus.Unpaid;
        public DateTime CreatedAt { get; set; }
        public DateTime? SettledAt { get; set; }

        public int? BookingId { get; set; }
        public virtual Booking Booking { get; set; }

        public int? RentalId { get; set; }
        public virtual Rental Rental { get; set; }

        public int? RentalRenewalId { get; set; }
        public virtual RentalRenewal RentalRenewal { get; set; }

        public int? SaleId { get; set; }
        public virtual Sale Sale { get; set; }

        public int? FineId { get; set; }
        public virtual Fine Fine { get; set; }

        public virtual ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
    }

    /// <summary>
    /// A card charge or a refund. Only the brand and last four digits of a card are ever kept (D-6).
    /// CheckoutToken makes a double-submitted checkout a no-op instead of a second charge.
    /// </summary>
    public class Payment
    {
        public int PaymentId { get; set; }

        [Required, StringLength(30), Index(IsUnique = true)]
        public string Reference { get; set; }

        public int StudentId { get; set; }
        public virtual Student Student { get; set; }

        public PaymentType Type { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Card;
        public PaymentStatus Status { get; set; }

        /// <summary>Always positive; Type says whether money came in or went back.</summary>
        public decimal Amount { get; set; }

        [StringLength(20)]
        public string CardBrand { get; set; }

        [StringLength(4)]
        public string CardLast4 { get; set; }

        [StringLength(50)]
        public string GatewayReference { get; set; }

        [StringLength(200)]
        public string FailureReason { get; set; }

        public Guid? CheckoutToken { get; set; }

        public int? RefundOfPaymentId { get; set; }
        public virtual Payment RefundOf { get; set; }

        [StringLength(200)]
        public string Note { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int? ProcessedByUserId { get; set; }
        public virtual User ProcessedBy { get; set; }

        public virtual ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
    }

    /// <summary>How much of a payment settled which fee. Allocations of a payment sum to its Amount.</summary>
    public class PaymentAllocation
    {
        [Key]
        public int AllocationId { get; set; }

        public int PaymentId { get; set; }
        public virtual Payment Payment { get; set; }

        public int FeeId { get; set; }
        public virtual Fee Fee { get; set; }

        public decimal Amount { get; set; }
    }
}
