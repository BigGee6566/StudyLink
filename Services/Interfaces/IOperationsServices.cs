using System;
using System.Collections.Generic;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;

namespace StudyLink.Services.Interfaces
{
    /// <summary>SC26 Supplier Orders - both the hub side (Manager/Staff) and the supplier portal.</summary>
    public interface ISupplierOrderService
    {
        // Hub side
        IList<OrderRow> Orders(Actor actor, SupplierOrderStatus? status);
        OrderDetail Get(Actor actor, int orderId);
        ServiceResult<int> Create(Actor actor, int supplierId, DateTime? expectedDate, string notes, IList<NewOrderLine> lines);
        ServiceResult Cancel(Actor actor, int orderId, string reason);
        IList<DeliveryRow> DeliveriesToReceive(Actor actor);

        /// <summary>Receives a dispatched delivery; this is where supplied stock enters the inventory.</summary>
        ServiceResult Receive(Actor actor, int deliveryId, IDictionary<int, int> acceptedByDeliveryLine, string note);
        ServiceResult RejectDelivery(Actor actor, int deliveryId, string reason);
        IList<InvoiceRow> Invoices(Actor actor, InvoiceStatus? status);
        ServiceResult ReviewInvoice(Actor actor, int invoiceId, InvoiceStatus newStatus, string note);

        // Supplier portal
        IList<OrderRow> MyOrders(Actor actor, bool openOnly);
        ServiceResult Acknowledge(Actor actor, int orderId);
        ServiceResult<int> Dispatch(Actor actor, int orderId, IDictionary<int, int> quantityByLine, string note);
        IList<DeliveryRow> MyDeliveries(Actor actor);
        ServiceResult<int> UploadInvoice(Actor actor, int orderId, string invoiceNumber, decimal amount, string fileName, byte[] content);
        IList<InvoiceRow> MyInvoices(Actor actor);
        IList<SuppliedTitleRow> SuppliedTitles(Actor actor);

        /// <summary>Invoice PDF for its supplier or an invoice reviewer; null otherwise.</summary>
        byte[] InvoiceFile(Actor actor, int invoiceId, out string fileName);
    }

    /// <summary>SC24 Reports Dashboard. Every figure is computed from live data.</summary>
    public interface IReportService
    {
        IList<ReportDefinition> Available(Actor actor);
        ReportResult Run(Actor actor, string key, DateTime? from, DateTime? to);
        string ToCsv(ReportResult result);
    }

    public interface IDashboardService
    {
        StudentDashboard Student(Actor actor);
        TutorDashboard Tutor(Actor actor);
        OperationsDashboard Staff(Actor actor);
        OperationsDashboard Manager(Actor actor);
        SupplierDashboard Supplier(Actor actor);
        AdminDashboard Admin(Actor actor);
    }

    public interface ISupportService
    {
        ServiceResult Submit(Actor actorOrNull, string name, string email, int? campusId, string subject, string body, string ipAddress);
        IList<SupportRow> List(Actor actor, SupportStatus? status);
        ServiceResult Respond(Actor actor, int messageId, SupportStatus status, string response);
    }

    public interface IBackupService
    {
        ServiceResult<int> Run(Actor actor);
        IList<BackupRow> History(Actor actor);
    }

    /// <summary>Hourly housekeeping; every step is idempotent so a repeated run changes nothing.</summary>
    public interface IMaintenanceService
    {
        SweepSummary Run();
    }
}
