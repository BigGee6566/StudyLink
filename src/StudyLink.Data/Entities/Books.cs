using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyLink.Data.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required, StringLength(80), Index(IsUnique = true)]
        public string Name { get; set; }

        [StringLength(300)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// A title. Stock is NOT a column: available / rented / sold / overdue counts are always
    /// derived from BookCopies so they cannot drift from reality.
    /// </summary>
    public class Book
    {
        public int BookId { get; set; }

        [Required, StringLength(20), Index(IsUnique = true)]
        public string Isbn { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        [Required, StringLength(200)]
        public string Authors { get; set; }

        [StringLength(30)]
        public string Edition { get; set; }

        [StringLength(100)]
        public string Publisher { get; set; }

        public int? PublishedYear { get; set; }

        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        /// <summary>Module the title is prescribed for (optional).</summary>
        public int? SubjectId { get; set; }
        public virtual Subject Subject { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public byte[] CoverImage { get; set; }

        [StringLength(50)]
        public string CoverContentType { get; set; }

        public decimal SalePrice { get; set; }
        public decimal WeeklyRentalFee { get; set; }
        public decimal LateFeePerDay { get; set; }

        /// <summary>Low-stock alert fires when available copies (all campuses) fall below this.</summary>
        public int ReorderLevel { get; set; }

        public bool IsRentable { get; set; } = true;
        public bool IsForSale { get; set; } = true;
        public bool IsActive { get; set; } = true;

        /// <summary>Supplier → Books.</summary>
        public int? PreferredSupplierId { get; set; }
        public virtual Supplier PreferredSupplier { get; set; }

        /// <summary>Manager → Books.</summary>
        public int? CreatedByManagerId { get; set; }
        public virtual Manager CreatedByManager { get; set; }

        public int? UpdatedByManagerId { get; set; }
        public virtual Manager UpdatedByManager { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<BookCopy> Copies { get; set; } = new List<BookCopy>();
    }

    public class BookCopy
    {
        [Key]
        public int CopyId { get; set; }

        public int BookId { get; set; }
        public virtual Book Book { get; set; }

        public int CampusId { get; set; }
        public virtual Campus Campus { get; set; }

        [Required, StringLength(30), Index(IsUnique = true)]
        public string Barcode { get; set; }

        public CopyCondition Condition { get; set; } = CopyCondition.New;
        public CopyStatus Status { get; set; } = CopyStatus.Available;
        public DateTime AcquiredAt { get; set; }

        /// <summary>Provenance: the supplier delivery line that brought this copy in, if any.</summary>
        public int? DeliveryLineId { get; set; }
        public virtual DeliveryLine DeliveryLine { get; set; }

        [StringLength(200)]
        public string Notes { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }
    }

    public class Rental
    {
        public int RentalId { get; set; }

        [Required, StringLength(20), Index(IsUnique = true)]
        public string Reference { get; set; }

        public int StudentId { get; set; }
        public virtual Student Student { get; set; }

        public int CopyId { get; set; }
        public virtual BookCopy Copy { get; set; }

        public DateTime RentedAt { get; set; }

        [Column(TypeName = "date")]
        public DateTime DueDate { get; set; }

        public DateTime? ReturnedAt { get; set; }
        public RentalStatus Status { get; set; }
        public decimal Fee { get; set; }
        public int RenewalCount { get; set; }
        public DateTime? HoldExpiresAt { get; set; }
        public CopyCondition? ReturnCondition { get; set; }

        public int? ProcessedByUserId { get; set; }
        public virtual User ProcessedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public virtual ICollection<RentalRenewal> Renewals { get; set; } = new List<RentalRenewal>();
        public virtual ICollection<Fine> Fines { get; set; } = new List<Fine>();
    }

    public class RentalRenewal
    {
        [Key]
        public int RenewalId { get; set; }

        public int RentalId { get; set; }
        public virtual Rental Rental { get; set; }

        public DateTime RenewedAt { get; set; }

        [Column(TypeName = "date")]
        public DateTime PreviousDueDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime NewDueDate { get; set; }

        public decimal Fee { get; set; }
    }

    public class Sale
    {
        public int SaleId { get; set; }

        [Required, StringLength(20), Index(IsUnique = true)]
        public string Reference { get; set; }

        public int StudentId { get; set; }
        public virtual Student Student { get; set; }

        public int CopyId { get; set; }
        public virtual BookCopy Copy { get; set; }

        public decimal Price { get; set; }
        public SaleStatus Status { get; set; }
        public DateTime? HoldExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>Rental → Fines. Each fine also creates a Fee so it appears on My Fees.</summary>
    public class Fine
    {
        public int FineId { get; set; }

        public int RentalId { get; set; }
        public virtual Rental Rental { get; set; }

        public FineType Type { get; set; }
        public decimal Amount { get; set; }
        public int DaysLate { get; set; }
        public FineStatus Status { get; set; } = FineStatus.Outstanding;

        [StringLength(200)]
        public string Reason { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? CreatedByUserId { get; set; }
        public virtual User CreatedBy { get; set; }

        public int? WaivedByUserId { get; set; }
        public virtual User WaivedBy { get; set; }

        public DateTime? WaivedAt { get; set; }

        [StringLength(200)]
        public string WaiverReason { get; set; }
    }

    /// <summary>SC11 CR14: join a waitlist for an out-of-stock title at a campus.</summary>
    public class BookWaitlist
    {
        [Key]
        public int WaitlistId { get; set; }

        [Index("UQ_BookWaitlist", 1, IsUnique = true)]
        public int BookId { get; set; }
        public virtual Book Book { get; set; }

        [Index("UQ_BookWaitlist", 2, IsUnique = true)]
        public int StudentId { get; set; }
        public virtual Student Student { get; set; }

        [Index("UQ_BookWaitlist", 3, IsUnique = true)]
        public int CampusId { get; set; }
        public virtual Campus Campus { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? NotifiedAt { get; set; }
    }
}
