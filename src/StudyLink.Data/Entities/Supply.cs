using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyLink.Data.Entities
{
    /// <summary>Supplier → Orders. Placed by a Manager, acknowledged and fulfilled by the Supplier (SC26).</summary>
    public class SupplierOrder
    {
        [Key]
        public int OrderId { get; set; }

        [Required, StringLength(20), Index(IsUnique = true)]
        public string Reference { get; set; }

        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        public int CreatedByManagerId { get; set; }
        public virtual Manager CreatedByManager { get; set; }

        public SupplierOrderStatus Status { get; set; } = SupplierOrderStatus.Placed;
        public DateTime OrderedAt { get; set; }

        [Column(TypeName = "date")]
        public DateTime? ExpectedDate { get; set; }

        public DateTime? AcknowledgedAt { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public virtual ICollection<SupplierOrderLine> Lines { get; set; } = new List<SupplierOrderLine>();
        public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    }

    public class SupplierOrderLine
    {
        [Key]
        public int LineId { get; set; }

        public int OrderId { get; set; }
        public virtual SupplierOrder Order { get; set; }

        public int BookId { get; set; }
        public virtual Book Book { get; set; }

        /// <summary>Campus the copies are for; received copies are created there.</summary>
        public int CampusId { get; set; }
        public virtual Campus Campus { get; set; }

        public int QuantityOrdered { get; set; }
        public int QuantityDelivered { get; set; }
        public decimal UnitCost { get; set; }
    }

    /// <summary>
    /// The supplier dispatches quantities ("update supplied stock"); Staff or a Manager receives
    /// the delivery, and only then are BookCopies created, so stock never rises on the supplier's
    /// word alone.
    /// </summary>
    public class Delivery
    {
        public int DeliveryId { get; set; }

        [Required, StringLength(20), Index(IsUnique = true)]
        public string Reference { get; set; }

        public int OrderId { get; set; }
        public virtual SupplierOrder Order { get; set; }

        public DeliveryStatus Status { get; set; } = DeliveryStatus.Dispatched;
        public DateTime DispatchedAt { get; set; }

        public int DispatchedByUserId { get; set; }
        public virtual User DispatchedBy { get; set; }

        public DateTime? ReceivedAt { get; set; }

        public int? ReceivedByUserId { get; set; }
        public virtual User ReceivedBy { get; set; }

        [StringLength(300)]
        public string Note { get; set; }

        public virtual ICollection<DeliveryLine> Lines { get; set; } = new List<DeliveryLine>();
    }

    public class DeliveryLine
    {
        public int DeliveryLineId { get; set; }

        public int DeliveryId { get; set; }
        public virtual Delivery Delivery { get; set; }

        public int OrderLineId { get; set; }
        public virtual SupplierOrderLine OrderLine { get; set; }

        public int Quantity { get; set; }
        public int? QuantityAccepted { get; set; }
    }

    /// <summary>Uploaded invoice PDF. The file lives in App_Data\Invoices (never under a public URL).</summary>
    public class SupplierInvoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Index("UQ_SupplierInvoice_Number", 1, IsUnique = true)]
        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        public int OrderId { get; set; }
        public virtual SupplierOrder Order { get; set; }

        [Required, StringLength(40), Index("UQ_SupplierInvoice_Number", 2, IsUnique = true)]
        public string InvoiceNumber { get; set; }

        public decimal Amount { get; set; }

        [Required, StringLength(200)]
        public string OriginalFileName { get; set; }

        [Required, StringLength(100)]
        public string StoredFileName { get; set; }

        public DateTime UploadedAt { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Submitted;

        public int? ReviewedByUserId { get; set; }
        public virtual User ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        [StringLength(300)]
        public string Note { get; set; }
    }
}
