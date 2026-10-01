using System;
using System.Collections.Generic;
using System.Data;
using StudyLink.Data.Entities;

namespace StudyLink.Services.DTOs
{
    // ---------------------------------------------------------------- Supply chain (SC18 / SC26)

    public sealed class NewOrderLine
    {
        public int BookId { get; set; }
        public int CampusId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }

    public sealed class OrderRow
    {
        public int OrderId { get; set; }
        public string Reference { get; set; }
        public int SupplierId { get; set; }
        public string Supplier { get; set; }
        public SupplierOrderStatus Status { get; set; }
        public DateTime OrderedAt { get; set; }
        public DateTime? ExpectedDate { get; set; }
        public int Lines { get; set; }
        public int UnitsOrdered { get; set; }
        public int UnitsDelivered { get; set; }
        public decimal Value { get; set; }
        public string CreatedBy { get; set; }
        public int PendingDeliveries { get; set; }
    }

    public sealed class OrderLineRow
    {
        public int LineId { get; set; }
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Isbn { get; set; }
        public string Campus { get; set; }
        public int QuantityOrdered { get; set; }
        public int QuantityDelivered { get; set; }
        public int QuantityInTransit { get; set; }
        public decimal UnitCost { get; set; }
        public int Outstanding => Math.Max(0, QuantityOrdered - QuantityDelivered - QuantityInTransit);
        public decimal LineTotal => QuantityOrdered * UnitCost;
    }

    public sealed class DeliveryLineRow
    {
        public int DeliveryLineId { get; set; }
        public int OrderLineId { get; set; }
        public string Title { get; set; }
        public string Campus { get; set; }
        public int Quantity { get; set; }
        public int? QuantityAccepted { get; set; }
    }

    public sealed class DeliveryRow
    {
        public int DeliveryId { get; set; }
        public string Reference { get; set; }
        public int OrderId { get; set; }
        public string OrderReference { get; set; }
        public string Supplier { get; set; }
        public DeliveryStatus Status { get; set; }
        public DateTime DispatchedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string ReceivedBy { get; set; }
        public int Units { get; set; }
        public string Note { get; set; }
        public IList<DeliveryLineRow> Lines { get; set; } = new List<DeliveryLineRow>();
    }

    public sealed class InvoiceRow
    {
        public int InvoiceId { get; set; }
        public int SupplierId { get; set; }
        public string Supplier { get; set; }
        public int OrderId { get; set; }
        public string OrderReference { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string Note { get; set; }
        public string FileName { get; set; }
    }

    public sealed class OrderDetail
    {
        public OrderRow Order { get; set; }
        public string Notes { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public IList<OrderLineRow> Lines { get; set; } = new List<OrderLineRow>();
        public IList<DeliveryRow> Deliveries { get; set; } = new List<DeliveryRow>();
        public IList<InvoiceRow> Invoices { get; set; } = new List<InvoiceRow>();
    }

    public sealed class SuppliedTitleRow
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Isbn { get; set; }
        public int Available { get; set; }
        public int ReorderLevel { get; set; }
        public int OnOrder { get; set; }
        public bool IsLow => Available < ReorderLevel;
    }

    // ---------------------------------------------------------------- Reports (SC24)

    public sealed class ReportDefinition
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool UsesDateRange { get; set; }
        public bool ManagerOnly { get; set; }
    }

    public sealed class ReportResult
    {
        public ReportDefinition Definition { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DataTable Table { get; set; }
        public IList<KeyValuePair<string, string>> Summary { get; set; } = new List<KeyValuePair<string, string>>();
    }

    // ---------------------------------------------------------------- Dashboards (SC6, SC10, SC18-SC21)

    public sealed class Kpi
    {
        public Kpi() { }

        public Kpi(string label, string value, string hint = null, string link = null, string tone = null)
        {
            Label = label;
            Value = value;
            Hint = hint;
            Link = link;
            Tone = tone;
        }

        public string Label { get; set; }
        public string Value { get; set; }
        public string Hint { get; set; }
        public string Link { get; set; }

        /// <summary>blue / green / orange / red / navy - maps to a card accent.</summary>
        public string Tone { get; set; }
    }

    public sealed class TrendPoint
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }

    public sealed class StudentDashboard
    {
        public string FirstName { get; set; }
        public string Reminder { get; set; }
        public decimal Outstanding { get; set; }
        public int UnpaidItems { get; set; }
        public IList<BookingRow> Upcoming { get; set; } = new List<BookingRow>();
        public IList<RentalRow> Rentals { get; set; } = new List<RentalRow>();
        public int ActiveRentals { get; set; }
        public int OverdueRentals { get; set; }
    }

    public sealed class TutorDashboard
    {
        public string FirstName { get; set; }
        public TutorVerificationStatus Status { get; set; }
        public int SessionsToday { get; set; }
        public decimal UtilisationPercent { get; set; }
        public int AwaitingResponse { get; set; }
        public int PendingPostponements { get; set; }
        public decimal EarningsThisMonth { get; set; }
        public decimal? AverageRating { get; set; }
        public IList<BookingRow> Today { get; set; } = new List<BookingRow>();
        public IList<BookingRow> Upcoming { get; set; } = new List<BookingRow>();
        public DateTime WeekStart { get; set; }
        public TimesheetStatus? TimesheetStatus { get; set; }
        public decimal TimesheetHours { get; set; }
        public int OpenSlots { get; set; }
    }

    public sealed class OperationsDashboard
    {
        public string FirstName { get; set; }
        public IList<Kpi> Kpis { get; set; } = new List<Kpi>();
        public IList<RentalRow> Overdue { get; set; } = new List<RentalRow>();
        public IList<VerificationRow> PendingTutors { get; set; } = new List<VerificationRow>();
        public IList<InventoryRow> LowStock { get; set; } = new List<InventoryRow>();
        public IList<DeliveryRow> DeliveriesToReceive { get; set; } = new List<DeliveryRow>();
        public IList<TrendPoint> RevenueTrend { get; set; } = new List<TrendPoint>();
        public IList<PaymentRow> RecentPayments { get; set; } = new List<PaymentRow>();
    }

    public sealed class SupplierDashboard
    {
        public string CompanyName { get; set; }
        public IList<Kpi> Kpis { get; set; } = new List<Kpi>();
        public IList<OrderRow> OpenOrders { get; set; } = new List<OrderRow>();
        public IList<InvoiceRow> RecentInvoices { get; set; } = new List<InvoiceRow>();
        public IList<SuppliedTitleRow> LowStockTitles { get; set; } = new List<SuppliedTitleRow>();
    }

    public sealed class AdminDashboard
    {
        public string FirstName { get; set; }
        public IList<Kpi> Kpis { get; set; } = new List<Kpi>();
        public IList<AuditRow> RecentAudit { get; set; } = new List<AuditRow>();
        public IList<RoleRow> Roles { get; set; } = new List<RoleRow>();
        public IList<UserRow> LockedAccounts { get; set; } = new List<UserRow>();
        public BackupRow LastBackup { get; set; }
        public string DatabaseName { get; set; }
        public string DatabaseSize { get; set; }
    }

    // ---------------------------------------------------------------- Support, backups, maintenance

    public sealed class SupportRow
    {
        public int MessageId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Campus { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public SupportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string HandledBy { get; set; }
        public string ResponseNote { get; set; }
        public bool FromSignedInUser { get; set; }
    }

    public sealed class BackupRow
    {
        public int BackupId { get; set; }
        public string FilePath { get; set; }
        public BackupStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long? SizeBytes { get; set; }
        public string RequestedBy { get; set; }
        public string Message { get; set; }
    }

    public sealed class SweepSummary
    {
        public int BookingsExpired { get; set; }
        public int HoldsReleased { get; set; }
        public int RemindersSent { get; set; }
        public int OverdueNotices { get; set; }
        public int LowStockAlerts { get; set; }
        public int TutorStatsRefreshed { get; set; }
        public bool MetricsCaptured { get; set; }
        public DateTime RanAt { get; set; }

        public override string ToString() =>
            $"expired {BookingsExpired} bookings, released {HoldsReleased} holds, {RemindersSent} due reminders, " +
            $"{OverdueNotices} overdue notices, {LowStockAlerts} low-stock alerts, {TutorStatsRefreshed} tutor stats";
    }
}
