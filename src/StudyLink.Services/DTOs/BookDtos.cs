using System;
using System.Collections.Generic;
using StudyLink.Data.Entities;
using StudyLink.Services.Policies;

namespace StudyLink.Services.DTOs
{
    public sealed class BookFilter
    {
        public string Text { get; set; }
        public int? CategoryId { get; set; }
        public int? SubjectId { get; set; }
        public int? CampusId { get; set; }
        public bool AvailableOnly { get; set; }
        public bool IncludeInactive { get; set; }
        public bool LowStockOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    /// <summary>SC11 list item.</summary>
    public sealed class BookCard
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Authors { get; set; }
        public string Edition { get; set; }
        public string Isbn { get; set; }
        public string Category { get; set; }
        public bool HasCover { get; set; }
        public decimal WeeklyRentalFee { get; set; }
        public decimal SalePrice { get; set; }
        public bool IsRentable { get; set; }
        public bool IsForSale { get; set; }
        public int Available { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class CampusStock
    {
        public int CampusId { get; set; }
        public string Campus { get; set; }
        public int Available { get; set; }
    }

    /// <summary>SC12 Book Details, including the rental terms the student is agreeing to.</summary>
    public sealed class BookDetail
    {
        public int BookId { get; set; }
        public string Isbn { get; set; }
        public string Title { get; set; }
        public string Authors { get; set; }
        public string Edition { get; set; }
        public string Publisher { get; set; }
        public int? PublishedYear { get; set; }
        public string Category { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public bool HasCover { get; set; }
        public decimal SalePrice { get; set; }
        public decimal WeeklyRentalFee { get; set; }
        public decimal RentalFee { get; set; }
        public decimal LateFeePerDay { get; set; }
        public bool IsRentable { get; set; }
        public bool IsForSale { get; set; }
        public bool IsActive { get; set; }
        public int RentalPeriodDays { get; set; }
        public DateTime DueDateIfRentedToday { get; set; }
        public int? CampusId { get; set; }
        public int AvailableAtCampus { get; set; }
        public IList<CampusStock> Stock { get; set; } = new List<CampusStock>();
        public bool OnWaitlist { get; set; }
        public string AlreadyHas { get; set; }
    }

    public sealed class BookEdit
    {
        public int? BookId { get; set; }
        public string Isbn { get; set; }
        public string Title { get; set; }
        public string Authors { get; set; }
        public string Edition { get; set; }
        public string Publisher { get; set; }
        public int? PublishedYear { get; set; }
        public int CategoryId { get; set; }
        public int? SubjectId { get; set; }
        public string Description { get; set; }
        public decimal SalePrice { get; set; }
        public decimal WeeklyRentalFee { get; set; }
        public decimal LateFeePerDay { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsRentable { get; set; } = true;
        public bool IsForSale { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public int? PreferredSupplierId { get; set; }
        public byte[] CoverImage { get; set; }
        public string CoverContentType { get; set; }
        public bool HasCover { get; set; }
    }

    /// <summary>SC25 inventory line: stock is derived from copy statuses.</summary>
    public sealed class InventoryRow
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Isbn { get; set; }
        public string Category { get; set; }
        public decimal SalePrice { get; set; }
        public decimal WeeklyRentalFee { get; set; }
        public bool IsActive { get; set; }
        public string PreferredSupplier { get; set; }
        public StockSummary Stock { get; set; }
    }

    public sealed class CopyRow
    {
        public int CopyId { get; set; }
        public string Barcode { get; set; }
        public int CampusId { get; set; }
        public string Campus { get; set; }
        public CopyCondition Condition { get; set; }
        public CopyStatus Status { get; set; }
        public DateTime AcquiredAt { get; set; }
        public string Holder { get; set; }
        public DateTime? DueDate { get; set; }
        public string Notes { get; set; }
        public string Source { get; set; }
    }

    public sealed class CategoryRow
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public int Books { get; set; }
    }

    public sealed class RentalFilter
    {
        public RentalStatus? Status { get; set; }
        public bool OverdueOnly { get; set; }
        public int? CampusId { get; set; }
        public string Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public sealed class RentalRow
    {
        public int RentalId { get; set; }
        public string Reference { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; }
        public string Barcode { get; set; }
        public string Campus { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public DateTime RentedAt { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public RentalStatus Status { get; set; }
        public int RenewalCount { get; set; }
        public decimal Fee { get; set; }
        public decimal LateFeePerDay { get; set; }
        public decimal SalePrice { get; set; }
        public DateTime? HoldExpiresAt { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
        public decimal AccruedFine { get; set; }
        public decimal FinesOutstanding { get; set; }
    }

    public sealed class ReturnRequest
    {
        public int RentalId { get; set; }
        public CopyCondition Condition { get; set; }
        public decimal DamageFine { get; set; }
        public string Notes { get; set; }
    }

    public sealed class ReturnResult
    {
        public int RentalId { get; set; }
        public string Reference { get; set; }
        public int DaysLate { get; set; }
        public decimal OverdueFine { get; set; }
        public decimal DamageFine { get; set; }
        public decimal TotalFines => OverdueFine + DamageFine;
        public bool WaitlistNotified { get; set; }
    }

    public sealed class FineRow
    {
        public int FineId { get; set; }
        public int RentalId { get; set; }
        public string RentalReference { get; set; }
        public string BookTitle { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public FineType Type { get; set; }
        public decimal Amount { get; set; }
        public int DaysLate { get; set; }
        public FineStatus Status { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public string WaiverReason { get; set; }
    }

    public sealed class SaleRow
    {
        public int SaleId { get; set; }
        public string Reference { get; set; }
        public string BookTitle { get; set; }
        public string Barcode { get; set; }
        public string StudentName { get; set; }
        public decimal Price { get; set; }
        public SaleStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? HoldExpiresAt { get; set; }
    }

    public sealed class WaitlistRow
    {
        public int WaitlistId { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; }
        public string Campus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? NotifiedAt { get; set; }
    }
}
