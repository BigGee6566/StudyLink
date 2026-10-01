using System;
using System.Collections.Generic;
using StudyLink.Data.Entities;

namespace StudyLink.Services.DTOs
{
    public sealed class FeeRow
    {
        public int FeeId { get; set; }
        public FeeType Type { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public FeeStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SettledAt { get; set; }
        public string SourceReference { get; set; }
        public DateTime? PayBy { get; set; }
    }

    /// <summary>SC14 My Fees.</summary>
    public sealed class FeeStatement
    {
        public decimal Outstanding { get; set; }
        public decimal PaidThisYear { get; set; }
        public IList<FeeRow> Unpaid { get; set; } = new List<FeeRow>();
        public IList<FeeRow> History { get; set; } = new List<FeeRow>();
        public IList<PaymentRow> Payments { get; set; } = new List<PaymentRow>();
    }

    public sealed class PaymentFilter
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public PaymentType? Type { get; set; }
        public PaymentStatus? Status { get; set; }
        public string Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public sealed class PaymentRow
    {
        public int PaymentId { get; set; }
        public string Reference { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public PaymentType Type { get; set; }
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
        public string Card { get; set; }
        public string Items { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FailureReason { get; set; }
    }

    public sealed class ReceiptLine
    {
        public int FeeId { get; set; }
        public FeeStatus Status { get; set; }
        public string Description { get; set; }
        public FeeType Type { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>SC13 / SC16 / SC17 confirmation and printable receipt.</summary>
    public sealed class Receipt
    {
        public int PaymentId { get; set; }
        public string Reference { get; set; }
        public PaymentType Type { get; set; }
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
        public string CardBrand { get; set; }
        public string CardLast4 { get; set; }
        public string GatewayReference { get; set; }
        public DateTime CreatedAt { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public string StudentEmail { get; set; }
        public string Organisation { get; set; }
        public decimal OutstandingAfter { get; set; }
        public IList<ReceiptLine> Lines { get; set; } = new List<ReceiptLine>();

        // What the payment bought, for the tailored confirmation screens.
        public int? BookingId { get; set; }
        public int? RentalId { get; set; }
        public int? SaleId { get; set; }
    }

    public sealed class CheckoutSummary
    {
        public IList<FeeRow> Fees { get; set; } = new List<FeeRow>();
        public decimal Total { get; set; }
        public DateTime? HoldExpiresAt { get; set; }
        public string Purpose { get; set; }
    }
}
