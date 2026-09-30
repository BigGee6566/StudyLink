using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StudyLink.Data;
using StudyLink.Data.Entities;
using StudyLink.Data.Seed;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Interfaces;
using StudyLink.Services.Policies;
using StudyLink.Services.Security;
using StudyLink.Services.Validation;

namespace StudyLink.Services.Implementations
{
    public sealed class SupplierOrderService : ISupplierOrderService
    {
        private const int MaxInvoiceBytes = 5 * 1024 * 1024;

        private readonly StudyLinkDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IFileStore _files;
        private readonly IClock _clock;

        public SupplierOrderService(StudyLinkDbContext db, IAuditService audit, INotificationService notifications, IFileStore files, IClock clock)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
            _files = files;
            _clock = clock;
        }

        private int RequireSupplier(Actor actor)
        {
            RolePermissions.Demand(actor, Permission.SupplierPortal);
            return _db.Suppliers.Where(s => s.UserId == actor.UserId).Select(s => (int?)s.SupplierId).FirstOrDefault()
                   ?? throw new AccessDeniedException("No supplier profile is linked to this account.");
        }

        private IQueryable<OrderRow> OrderRows(IQueryable<SupplierOrder> q) =>
            q.AsNoTracking().OrderByDescending(o => o.OrderedAt).Select(o => new OrderRow
            {
                OrderId = o.OrderId, Reference = o.Reference, SupplierId = o.SupplierId, Supplier = o.Supplier.CompanyName,
                Status = o.Status, OrderedAt = o.OrderedAt, ExpectedDate = o.ExpectedDate, Lines = o.Lines.Count(),
                UnitsOrdered = o.Lines.Sum(l => (int?)l.QuantityOrdered) ?? 0,
                UnitsDelivered = o.Lines.Sum(l => (int?)l.QuantityDelivered) ?? 0,
                Value = o.Lines.Sum(l => (decimal?)(l.QuantityOrdered * l.UnitCost)) ?? 0,
                CreatedBy = o.CreatedByManager.User.FirstName + " " + o.CreatedByManager.User.LastName,
                PendingDeliveries = o.Deliveries.Count(d => d.Status == DeliveryStatus.Dispatched)
            });

        private IQueryable<DeliveryRow> DeliveryRows(IQueryable<Delivery> q) =>
            q.AsNoTracking().OrderByDescending(d => d.DispatchedAt).Select(d => new DeliveryRow
            {
                DeliveryId = d.DeliveryId, Reference = d.Reference, OrderId = d.OrderId, OrderReference = d.Order.Reference,
                Supplier = d.Order.Supplier.CompanyName, Status = d.Status, DispatchedAt = d.DispatchedAt, ReceivedAt = d.ReceivedAt,
                ReceivedBy = d.ReceivedBy == null ? null : d.ReceivedBy.FirstName + " " + d.ReceivedBy.LastName,
                Units = d.Lines.Sum(l => (int?)l.Quantity) ?? 0, Note = d.Note,
                Lines = d.Lines.Select(l => new DeliveryLineRow
                {
                    DeliveryLineId = l.DeliveryLineId, OrderLineId = l.OrderLineId, Title = l.OrderLine.Book.Title,
                    Campus = l.OrderLine.Campus.Name, Quantity = l.Quantity, QuantityAccepted = l.QuantityAccepted
                }).ToList()
            });

        private IQueryable<InvoiceRow> InvoiceRows(IQueryable<SupplierInvoice> q) =>
            q.AsNoTracking().OrderByDescending(i => i.UploadedAt).Select(i => new InvoiceRow
            {
                InvoiceId = i.InvoiceId, SupplierId = i.SupplierId, Supplier = i.Supplier.CompanyName, OrderId = i.OrderId,
                OrderReference = i.Order.Reference, InvoiceNumber = i.InvoiceNumber, Amount = i.Amount, Status = i.Status,
                UploadedAt = i.UploadedAt, PaidAt = i.PaidAt, Note = i.Note, FileName = i.OriginalFileName
            });

        // ------------------------------------------------------------------ hub side

        public IList<OrderRow> Orders(Actor actor, SupplierOrderStatus? status)
        {
            if (!RolePermissions.Has(actor, Permission.ManageSupplierOrders) && !RolePermissions.Has(actor, Permission.ReceiveDeliveries))
                throw new AccessDeniedException("You cannot view supplier orders.");
            return OrderRows(_db.SupplierOrders.Where(o => !status.HasValue || o.Status == status.Value)).Take(300).ToList();
        }

        public OrderDetail Get(Actor actor, int orderId)
        {
            var order = _db.SupplierOrders.AsNoTracking().FirstOrDefault(o => o.OrderId == orderId);
            if (order == null) return null;
            var isSupplier = _db.Suppliers.Any(s => s.SupplierId == order.SupplierId && s.UserId == actor.UserId);
            if (!isSupplier && !RolePermissions.Has(actor, Permission.ManageSupplierOrders) && !RolePermissions.Has(actor, Permission.ReceiveDeliveries))
                return null;

            return new OrderDetail
            {
                Order = OrderRows(_db.SupplierOrders.Where(o => o.OrderId == orderId)).Single(),
                Notes = order.Notes,
                AcknowledgedAt = order.AcknowledgedAt,
                Lines = _db.SupplierOrderLines.AsNoTracking().Where(l => l.OrderId == orderId).OrderBy(l => l.LineId)
                    .Select(l => new OrderLineRow
                    {
                        LineId = l.LineId, BookId = l.BookId, Title = l.Book.Title, Isbn = l.Book.Isbn, Campus = l.Campus.Name,
                        QuantityOrdered = l.QuantityOrdered, QuantityDelivered = l.QuantityDelivered, UnitCost = l.UnitCost,
                        QuantityInTransit = _db.DeliveryLines.Where(d => d.OrderLineId == l.LineId && d.Delivery.Status == DeliveryStatus.Dispatched)
                            .Sum(d => (int?)d.Quantity) ?? 0
                    }).ToList(),
                Deliveries = DeliveryRows(_db.Deliveries.Where(d => d.OrderId == orderId)).ToList(),
                Invoices = InvoiceRows(_db.SupplierInvoices.Where(i => i.OrderId == orderId)).ToList()
            };
        }

        public ServiceResult<int> Create(Actor actor, int supplierId, DateTime? expectedDate, string notes, IList<NewOrderLine> lines)
        {
            RolePermissions.Demand(actor, Permission.ManageSupplierOrders);
            var managerId = _db.Managers.Where(m => m.UserId == actor.UserId).Select(m => (int?)m.ManagerId).FirstOrDefault();
            if (managerId == null) return ServiceResult<int>.Fail("Only a manager can place supplier orders.");
            var supplier = _db.Suppliers.Include(s => s.User).FirstOrDefault(s => s.SupplierId == supplierId);
            if (supplier == null || !supplier.User.IsActive) return ServiceResult<int>.Fail("Choose an active supplier.");
            if (expectedDate.HasValue && expectedDate.Value.Date < _clock.Today) return ServiceResult<int>.Fail("The expected date cannot be in the past.");
            if (TextRules.TooLong(notes, 500)) return ServiceResult<int>.Fail("Notes are limited to 500 characters.");

            var merged = (lines ?? new List<NewOrderLine>()).Where(l => l.Quantity != 0)
                .GroupBy(l => new { l.BookId, l.CampusId })
                .Select(g => new NewOrderLine { BookId = g.Key.BookId, CampusId = g.Key.CampusId, Quantity = g.Sum(x => x.Quantity), UnitCost = g.First().UnitCost })
                .ToList();
            if (merged.Count == 0 || merged.Count > 50) return ServiceResult<int>.Fail("An order needs between 1 and 50 lines.");
            foreach (var l in merged)
            {
                if (l.Quantity < 1 || l.Quantity > 1000) return ServiceResult<int>.Fail("Quantities must be between 1 and 1000.");
                if (l.UnitCost < 0 || l.UnitCost > 100000) return ServiceResult<int>.Fail("Unit costs must be between R0 and R100 000.");
                if (!_db.Books.Any(b => b.BookId == l.BookId && b.IsActive)) return ServiceResult<int>.Fail("One of the books is not in the catalogue.");
                if (!_db.Campuses.Any(c => c.CampusId == l.CampusId && c.IsActive)) return ServiceResult<int>.Fail("Choose a delivery campus for every line.");
            }

            var now = _clock.Now;
            var order = _db.SupplierOrders.Add(new SupplierOrder
            {
                Reference = ReferenceGenerator.New("PO", now), SupplierId = supplierId, CreatedByManagerId = managerId.Value,
                Status = SupplierOrderStatus.Placed, OrderedAt = now, ExpectedDate = expectedDate?.Date, Notes = TextRules.Clean(notes, 500)
            });
            foreach (var l in merged)
                order.Lines.Add(new SupplierOrderLine { BookId = l.BookId, CampusId = l.CampusId, QuantityOrdered = l.Quantity, UnitCost = MoneyPolicy.Round(l.UnitCost) });
            _db.SaveChanges();

            _audit.Record(actor, "SupplierOrderCreated", "SupplierOrder", order.Reference, newState: supplier.CompanyName + ", " + merged.Sum(l => l.Quantity) + " units");
            _notifications.Notify(supplier.UserId, NotificationType.SupplierOrderCreated, "New purchase order",
                "Forte Books & Tuition Hub placed order " + order.Reference + " for " + merged.Sum(l => l.Quantity) + " books.",
                "~/Supplier/OrderDetails.aspx?id=" + order.OrderId);
            _db.SaveChanges();
            return ServiceResult<int>.Ok(order.OrderId);
        }

        public ServiceResult Cancel(Actor actor, int orderId, string reason)
        {
            RolePermissions.Demand(actor, Permission.ManageSupplierOrders);
            if (string.IsNullOrWhiteSpace(reason)) return ServiceResult.Fail("Give a reason for cancelling the order.");
            var order = _db.SupplierOrders.Include(o => o.Supplier).FirstOrDefault(o => o.OrderId == orderId);
            if (order == null) return ServiceResult.Fail("Order not found.");
            if (order.Status != SupplierOrderStatus.Placed && order.Status != SupplierOrderStatus.Acknowledged)
                return ServiceResult.Fail("Only an order with no deliveries can be cancelled.");
            if (_db.Deliveries.Any(d => d.OrderId == orderId && d.Status == DeliveryStatus.Dispatched))
                return ServiceResult.Fail("A delivery for this order is on its way. Receive or reject it first.");

            var previous = order.Status;
            order.Status = SupplierOrderStatus.Cancelled;
            _audit.Record(actor, "SupplierOrderCancelled", "SupplierOrder", order.Reference, previous.ToString(), "Cancelled", reason.Trim());
            _notifications.Notify(order.Supplier.UserId, NotificationType.SupplierOrderCreated, "Order cancelled",
                "Order " + order.Reference + " was cancelled: " + reason.Trim(), "~/Supplier/OrderDetails.aspx?id=" + orderId);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public IList<DeliveryRow> DeliveriesToReceive(Actor actor)
        {
            RolePermissions.Demand(actor, Permission.ReceiveDeliveries);
            return DeliveryRows(_db.Deliveries.Where(d => d.Status == DeliveryStatus.Dispatched)).ToList();
        }

        public ServiceResult Receive(Actor actor, int deliveryId, IDictionary<int, int> accepted, string note)
        {
            RolePermissions.Demand(actor, Permission.ReceiveDeliveries);
            var delivery = _db.Deliveries.Include(d => d.Lines.Select(l => l.OrderLine)).Include(d => d.Order.Supplier)
                .FirstOrDefault(d => d.DeliveryId == deliveryId);
            if (delivery == null) return ServiceResult.Fail("Delivery not found.");
            if (delivery.Status != DeliveryStatus.Dispatched) return ServiceResult.Fail("This delivery has already been processed.");
            if (TextRules.TooLong(note, 300)) return ServiceResult.Fail("The note is limited to 300 characters.");

            foreach (var line in delivery.Lines)
            {
                var qty = accepted != null && accepted.TryGetValue(line.DeliveryLineId, out var a) ? a : line.Quantity;
                if (qty < 0 || qty > line.Quantity) return ServiceResult.Fail("Accepted quantities must be between 0 and the quantity delivered.");
                if (line.OrderLine.QuantityDelivered + qty > line.OrderLine.QuantityOrdered)
                    return ServiceResult.Fail("Accepting this would exceed the quantity ordered.");
                line.QuantityAccepted = qty;
            }

            var now = _clock.Now;
            var units = 0;
            var restocked = new HashSet<Tuple<int, int>>();
            foreach (var line in delivery.Lines.Where(l => l.QuantityAccepted > 0))
            {
                // Supplied stock enters the inventory only here, once hub staff have counted it.
                CopyFactory.Create(_db, line.OrderLine.BookId, line.OrderLine.CampusId, line.QuantityAccepted.Value, CopyCondition.New, now,
                    "Delivery " + delivery.Reference, line);
                line.OrderLine.QuantityDelivered += line.QuantityAccepted.Value;
                units += line.QuantityAccepted.Value;
                restocked.Add(Tuple.Create(line.OrderLine.BookId, line.OrderLine.CampusId));
            }

            delivery.Status = DeliveryStatus.Received;
            delivery.ReceivedAt = now;
            delivery.ReceivedByUserId = actor.UserId;
            if (!string.IsNullOrWhiteSpace(note)) delivery.Note = (delivery.Note == null ? "" : delivery.Note + " | ") + "Hub: " + note.Trim();

            var order = delivery.Order;
            var orderLines = _db.SupplierOrderLines.Where(l => l.OrderId == order.OrderId).ToList();
            order.Status = orderLines.All(l => l.QuantityDelivered >= l.QuantityOrdered) ? SupplierOrderStatus.Delivered : SupplierOrderStatus.PartiallyDelivered;

            _audit.Record(actor, "DeliveryReceived", "Delivery", delivery.Reference, "Dispatched", "Received", units + " copies added to stock");
            _notifications.Notify(order.Supplier.UserId, NotificationType.SupplierDeliveryReceived, "Delivery received",
                "The hub received " + units + " of " + delivery.Lines.Sum(l => l.Quantity) + " books in delivery " + delivery.Reference + ".",
                "~/Supplier/OrderDetails.aspx?id=" + order.OrderId);
            _db.SaveChanges();

            foreach (var r in restocked)
                InventoryService.NotifyWaitlist(_db, _notifications, r.Item1, r.Item2, now);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult RejectDelivery(Actor actor, int deliveryId, string reason)
        {
            RolePermissions.Demand(actor, Permission.ReceiveDeliveries);
            if (string.IsNullOrWhiteSpace(reason)) return ServiceResult.Fail("Give a reason for rejecting the delivery.");
            var delivery = _db.Deliveries.Include(d => d.Order.Supplier).Include(d => d.Lines).FirstOrDefault(d => d.DeliveryId == deliveryId);
            if (delivery == null) return ServiceResult.Fail("Delivery not found.");
            if (delivery.Status != DeliveryStatus.Dispatched) return ServiceResult.Fail("This delivery has already been processed.");
            delivery.Status = DeliveryStatus.Rejected;
            delivery.ReceivedAt = _clock.Now;
            delivery.ReceivedByUserId = actor.UserId;
            delivery.Note = AuditService.Truncate((delivery.Note == null ? "" : delivery.Note + " | ") + "Rejected: " + reason.Trim(), 300);
            foreach (var l in delivery.Lines) l.QuantityAccepted = 0;
            _audit.Record(actor, "DeliveryRejected", "Delivery", delivery.Reference, "Dispatched", "Rejected", reason.Trim());
            _notifications.Notify(delivery.Order.Supplier.UserId, NotificationType.SupplierDeliveryReceived, "Delivery rejected",
                "Delivery " + delivery.Reference + " was rejected: " + reason.Trim(), "~/Supplier/OrderDetails.aspx?id=" + delivery.OrderId);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public IList<InvoiceRow> Invoices(Actor actor, InvoiceStatus? status)
        {
            RolePermissions.Demand(actor, Permission.ReviewInvoices);
            return InvoiceRows(_db.SupplierInvoices.Where(i => !status.HasValue || i.Status == status.Value)).Take(300).ToList();
        }

        public ServiceResult ReviewInvoice(Actor actor, int invoiceId, InvoiceStatus newStatus, string note)
        {
            RolePermissions.Demand(actor, Permission.ReviewInvoices);
            var invoice = _db.SupplierInvoices.Include(i => i.Supplier).FirstOrDefault(i => i.InvoiceId == invoiceId);
            if (invoice == null) return ServiceResult.Fail("Invoice not found.");
            var allowed = (invoice.Status == InvoiceStatus.Submitted && (newStatus == InvoiceStatus.Approved || newStatus == InvoiceStatus.Rejected))
                          || (invoice.Status == InvoiceStatus.Approved && newStatus == InvoiceStatus.Paid);
            if (!allowed) return ServiceResult.Fail("That status change is not allowed (Submitted → Approved/Rejected → Paid).");
            if (newStatus == InvoiceStatus.Rejected && string.IsNullOrWhiteSpace(note)) return ServiceResult.Fail("Tell the supplier why the invoice is rejected.");

            var previous = invoice.Status;
            invoice.Status = newStatus;
            invoice.ReviewedByUserId = actor.UserId;
            invoice.ReviewedAt = _clock.Now;
            if (newStatus == InvoiceStatus.Paid) invoice.PaidAt = _clock.Now;
            if (!string.IsNullOrWhiteSpace(note)) invoice.Note = TextRules.Clean(note, 300);
            _audit.Record(actor, "InvoiceStatusChanged", "SupplierInvoice", invoice.InvoiceNumber, previous.ToString(), newStatus.ToString(), invoice.Note);
            _notifications.Notify(invoice.Supplier.UserId, NotificationType.InvoiceStatusChanged, "Invoice " + newStatus.ToString().ToLowerInvariant(),
                "Invoice " + invoice.InvoiceNumber + " (R " + invoice.Amount.ToString("N2") + ") is now " + newStatus + (invoice.Note == null ? "." : ": " + invoice.Note),
                "~/Supplier/Invoices.aspx");
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        // ------------------------------------------------------------------ supplier portal

        public IList<OrderRow> MyOrders(Actor actor, bool openOnly)
        {
            var supplierId = RequireSupplier(actor);
            var q = _db.SupplierOrders.Where(o => o.SupplierId == supplierId);
            if (openOnly)
                q = q.Where(o => o.Status == SupplierOrderStatus.Placed || o.Status == SupplierOrderStatus.Acknowledged || o.Status == SupplierOrderStatus.PartiallyDelivered);
            return OrderRows(q).ToList();
        }

        public ServiceResult Acknowledge(Actor actor, int orderId)
        {
            var supplierId = RequireSupplier(actor);
            var order = _db.SupplierOrders.FirstOrDefault(o => o.OrderId == orderId && o.SupplierId == supplierId);
            if (order == null) return ServiceResult.Fail("Order not found.");
            if (order.Status != SupplierOrderStatus.Placed) return ServiceResult.Fail("This order has already been acknowledged.");
            order.Status = SupplierOrderStatus.Acknowledged;
            order.AcknowledgedAt = _clock.Now;
            _audit.Record(actor, "SupplierOrderAcknowledged", "SupplierOrder", order.Reference);
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public ServiceResult<int> Dispatch(Actor actor, int orderId, IDictionary<int, int> quantityByLine, string note)
        {
            var supplierId = RequireSupplier(actor);
            var detail = Get(actor, orderId);
            if (detail == null || detail.Order.SupplierId != supplierId) return ServiceResult<int>.Fail("Order not found.");
            var status = detail.Order.Status;
            if (status == SupplierOrderStatus.Cancelled || status == SupplierOrderStatus.Delivered)
                return ServiceResult<int>.Fail("This order is closed.");
            if (TextRules.TooLong(note, 300)) return ServiceResult<int>.Fail("The note is limited to 300 characters.");

            var lines = new List<DeliveryLine>();
            foreach (var line in detail.Lines)
            {
                var qty = quantityByLine != null && quantityByLine.TryGetValue(line.LineId, out var q) ? q : 0;
                if (qty < 0 || qty > line.Outstanding)
                    return ServiceResult<int>.Fail("\"" + line.Title + "\": you can dispatch at most " + line.Outstanding + " more.");
                if (qty > 0) lines.Add(new DeliveryLine { OrderLineId = line.LineId, Quantity = qty });
            }
            if (lines.Count == 0) return ServiceResult<int>.Fail("Enter the quantity you are delivering for at least one line.");

            var now = _clock.Now;
            var order = _db.SupplierOrders.Find(orderId);
            if (order.Status == SupplierOrderStatus.Placed)
            {
                order.Status = SupplierOrderStatus.Acknowledged;
                order.AcknowledgedAt = now;
            }
            var delivery = _db.Deliveries.Add(new Delivery
            {
                Reference = ReferenceGenerator.New("DL", now), OrderId = orderId, Status = DeliveryStatus.Dispatched,
                DispatchedAt = now, DispatchedByUserId = actor.UserId, Note = TextRules.Clean(note, 300)
            });
            foreach (var l in lines) delivery.Lines.Add(l);
            _audit.Record(actor, "DeliveryDispatched", "Delivery", delivery.Reference, newState: lines.Sum(l => l.Quantity) + " units for " + order.Reference);
            _notifications.NotifyHolders(Permission.ReceiveDeliveries, NotificationType.SupplierDeliveryDispatched, "Delivery on its way",
                detail.Order.Supplier + " dispatched " + lines.Sum(l => l.Quantity) + " books for " + order.Reference + ".",
                "~/Operations/Deliveries.aspx");
            _db.SaveChanges();
            return ServiceResult<int>.Ok(delivery.DeliveryId);
        }

        public IList<DeliveryRow> MyDeliveries(Actor actor)
        {
            var supplierId = RequireSupplier(actor);
            return DeliveryRows(_db.Deliveries.Where(d => d.Order.SupplierId == supplierId)).Take(200).ToList();
        }

        public ServiceResult<int> UploadInvoice(Actor actor, int orderId, string invoiceNumber, decimal amount, string fileName, byte[] content)
        {
            var supplierId = RequireSupplier(actor);
            var order = _db.SupplierOrders.FirstOrDefault(o => o.OrderId == orderId && o.SupplierId == supplierId);
            if (order == null) return ServiceResult<int>.Fail("Order not found.");
            if (order.Status == SupplierOrderStatus.Cancelled) return ServiceResult<int>.Fail("You cannot invoice a cancelled order.");
            invoiceNumber = (invoiceNumber ?? string.Empty).Trim();
            if (invoiceNumber.Length == 0 || invoiceNumber.Length > 40) return ServiceResult<int>.Fail("Enter your invoice number (40 characters max).");
            if (amount <= 0 || amount > 10000000) return ServiceResult<int>.Fail("Enter the invoice amount.");
            if (content == null || content.Length == 0) return ServiceResult<int>.Fail("Attach the invoice as a PDF.");
            if (content.Length > MaxInvoiceBytes) return ServiceResult<int>.Fail("Invoices must be smaller than 5 MB.");
            // Check the file's own signature, not just its name: only real PDFs are accepted.
            if (!string.Equals(Path.GetExtension(fileName ?? string.Empty), ".pdf", StringComparison.OrdinalIgnoreCase)
                || content.Length < 5 || Encoding.ASCII.GetString(content, 0, 5) != "%PDF-")
                return ServiceResult<int>.Fail("Only PDF invoices are accepted.");
            if (_db.SupplierInvoices.Any(i => i.SupplierId == supplierId && i.InvoiceNumber == invoiceNumber))
                return ServiceResult<int>.Fail("You have already uploaded an invoice with this number.");

            var stored = _files.Save("Invoices", content, ".pdf");
            var invoice = _db.SupplierInvoices.Add(new SupplierInvoice
            {
                SupplierId = supplierId, OrderId = orderId, InvoiceNumber = invoiceNumber, Amount = MoneyPolicy.Round(amount),
                OriginalFileName = AuditService.Truncate(Path.GetFileName(fileName), 200), StoredFileName = stored,
                UploadedAt = _clock.Now, Status = InvoiceStatus.Submitted
            });
            _audit.Record(actor, "InvoiceUploaded", "SupplierInvoice", invoiceNumber, newState: "R " + invoice.Amount.ToString("0.00", CultureInfo.InvariantCulture));
            _notifications.NotifyHolders(Permission.ReviewInvoices, NotificationType.InvoiceSubmitted, "Supplier invoice received",
                "Invoice " + invoiceNumber + " for " + order.Reference + " (R " + invoice.Amount.ToString("N2") + ").", "~/Manager/SupplierOrders.aspx?tab=invoices");
            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                return ServiceResult<int>.Fail("You have already uploaded an invoice with this number.");
            }
            return ServiceResult<int>.Ok(invoice.InvoiceId);
        }

        public IList<InvoiceRow> MyInvoices(Actor actor)
        {
            var supplierId = RequireSupplier(actor);
            return InvoiceRows(_db.SupplierInvoices.Where(i => i.SupplierId == supplierId)).ToList();
        }

        public IList<SuppliedTitleRow> SuppliedTitles(Actor actor)
        {
            var supplierId = RequireSupplier(actor);
            var open = new[] { SupplierOrderStatus.Placed, SupplierOrderStatus.Acknowledged, SupplierOrderStatus.PartiallyDelivered };
            return _db.Books.AsNoTracking().Where(b => b.IsActive && (b.PreferredSupplierId == supplierId
                                                                      || _db.SupplierOrderLines.Any(l => l.BookId == b.BookId && l.Order.SupplierId == supplierId)))
                .OrderBy(b => b.Title).Select(b => new SuppliedTitleRow
                {
                    BookId = b.BookId, Title = b.Title, Isbn = b.Isbn, ReorderLevel = b.ReorderLevel,
                    Available = b.Copies.Count(c => c.Status == CopyStatus.Available),
                    OnOrder = _db.SupplierOrderLines.Where(l => l.BookId == b.BookId && l.Order.SupplierId == supplierId && open.Contains(l.Order.Status))
                        .Sum(l => (int?)(l.QuantityOrdered - l.QuantityDelivered)) ?? 0
                }).ToList();
        }

        public byte[] InvoiceFile(Actor actor, int invoiceId, out string fileName)
        {
            fileName = null;
            var invoice = _db.SupplierInvoices.AsNoTracking().Include(i => i.Supplier).FirstOrDefault(i => i.InvoiceId == invoiceId);
            if (invoice == null) return null;
            if (invoice.Supplier.UserId != actor.UserId && !RolePermissions.Has(actor, Permission.ReviewInvoices)) return null;
            fileName = invoice.OriginalFileName;
            return _files.Read("Invoices", invoice.StoredFileName);
        }
    }

    public sealed class SupportService : ISupportService
    {
        private readonly StudyLinkDbContext _db;
        private readonly INotificationService _notifications;
        private readonly IEmailSender _email;
        private readonly IClock _clock;
        private readonly RateLimiter _limiter;

        public SupportService(StudyLinkDbContext db, INotificationService notifications, IEmailSender email, IClock clock, RateLimiter limiter = null)
        {
            _db = db;
            _notifications = notifications;
            _email = email;
            _clock = clock;
            _limiter = limiter ?? RateLimiter.Contact;
        }

        public ServiceResult Submit(Actor actorOrNull, string name, string email, int? campusId, string subject, string body, string ipAddress)
        {
            if (!_limiter.TryAcquire("contact:" + ipAddress))
                return ServiceResult.Fail("You have sent several messages recently. Please wait a few minutes.");
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100) return ServiceResult.Fail("Enter your name.");
            if (!UserValidator.IsValidEmail(email)) return ServiceResult.Fail("Enter a valid email address so we can reply.");
            if (string.IsNullOrWhiteSpace(subject) || subject.Trim().Length > 150) return ServiceResult.Fail("Enter a subject (150 characters max).");
            if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > 2000) return ServiceResult.Fail("Enter your message (2000 characters max).");
            if (campusId.HasValue && !_db.Campuses.Any(c => c.CampusId == campusId)) campusId = null;

            _db.SupportMessages.Add(new SupportMessage
            {
                UserId = actorOrNull?.UserId, Name = name.Trim(), Email = email.Trim(), CampusId = campusId,
                Subject = subject.Trim(), Body = body.Trim(), CreatedAt = _clock.Now
            });
            _notifications.NotifyHolders(Permission.HandleSupport, NotificationType.SupportMessageReceived, "New support message",
                name.Trim() + ": " + subject.Trim(), "~/Staff/Support.aspx");
            _db.SaveChanges();
            return ServiceResult.Ok();
        }

        public IList<SupportRow> List(Actor actor, SupportStatus? status)
        {
            RolePermissions.Demand(actor, Permission.HandleSupport);
            return _db.SupportMessages.AsNoTracking().Where(m => !status.HasValue || m.Status == status.Value)
                .OrderBy(m => m.Status).ThenByDescending(m => m.CreatedAt).Take(300)
                .Select(m => new SupportRow
                {
                    MessageId = m.MessageId, Name = m.Name, Email = m.Email, Campus = m.Campus == null ? null : m.Campus.Name,
                    Subject = m.Subject, Body = m.Body, Status = m.Status, CreatedAt = m.CreatedAt,
                    HandledBy = m.HandledBy == null ? null : m.HandledBy.FirstName + " " + m.HandledBy.LastName,
                    ResponseNote = m.ResponseNote, FromSignedInUser = m.UserId != null
                }).ToList();
        }

        public ServiceResult Respond(Actor actor, int messageId, SupportStatus status, string response)
        {
            RolePermissions.Demand(actor, Permission.HandleSupport);
            var m = _db.SupportMessages.Find(messageId);
            if (m == null) return ServiceResult.Fail("Message not found.");
            if (TextRules.TooLong(response, 500)) return ServiceResult.Fail("Responses are limited to 500 characters.");
            m.Status = status;
            m.HandledByUserId = actor.UserId;
            m.HandledAt = _clock.Now;
            if (!string.IsNullOrWhiteSpace(response)) m.ResponseNote = response.Trim();
            _db.SaveChanges();
            if (!string.IsNullOrWhiteSpace(response))
                _email.Send(m.Email, "Re: " + m.Subject,
                    "<p>Hello " + Accounts.Html(m.Name) + ",</p><p>" + Accounts.Html(response.Trim()) + "</p><p>– StudyLink support, Forte Books &amp; Tuition Hub</p>");
            return ServiceResult.Ok();
        }
    }

    public sealed class BackupService : IBackupService
    {
        private readonly StudyLinkDbContext _db;
        private readonly ISettingsService _settings;
        private readonly IAuditService _audit;
        private readonly IClock _clock;

        public BackupService(StudyLinkDbContext db, ISettingsService settings, IAuditService audit, IClock clock)
        {
            _db = db;
            _settings = settings;
            _audit = audit;
            _clock = clock;
        }

        public ServiceResult<int> Run(Actor actor)
        {
            RolePermissions.Demand(actor, Permission.ManageBackups);
            var database = _db.Database.Connection.Database;
            var folder = _settings.GetString(SettingKeys.BackupDirectory, string.Empty).Trim();
            if (folder.Length == 0)
                folder = _db.Database.SqlQuery<string>("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(400))").FirstOrDefault() ?? string.Empty;
            if (folder.Length == 0)
                return ServiceResult<int>.Fail("No backup folder is configured. Set Backup.Directory in System Settings.");

            var now = _clock.Now;
            var path = Path.Combine(folder, database + "_" + now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".bak");
            var log = _db.BackupLogs.Add(new BackupLog { FilePath = path, Status = BackupStatus.Running, StartedAt = now, RequestedByUserId = actor.UserId });
            _db.SaveChanges();

            var previousTimeout = _db.Database.CommandTimeout;
            try
            {
                _db.Database.CommandTimeout = 600;
                // BACKUP cannot run inside a transaction, so EF must not wrap this command in one.
                _db.Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction,
                    "BACKUP DATABASE [" + database.Replace("]", "]]") + "] TO DISK = @p0 WITH COPY_ONLY, INIT, CHECKSUM, NAME = N'StudyLink manual backup'", path);
                log.Status = BackupStatus.Succeeded;
                log.SizeBytes = _db.Database.SqlQuery<decimal?>(
                    "SELECT TOP (1) backup_size FROM msdb.dbo.backupset WHERE database_name = @p0 ORDER BY backup_finish_date DESC", database)
                    .FirstOrDefault() is decimal size ? (long)size : (long?)null;
                log.Message = "Backup completed.";
            }
            catch (Exception ex)
            {
                log.Status = BackupStatus.Failed;
                log.Message = AuditService.Truncate(ex.GetBaseException().Message, 500);
            }
            finally
            {
                _db.Database.CommandTimeout = previousTimeout;
            }

            log.CompletedAt = _clock.Now;
            _audit.Record(actor, log.Status == BackupStatus.Succeeded ? "BackupCreated" : "BackupFailed", "Backup", log.BackupId, details: path);
            _db.SaveChanges();
            return log.Status == BackupStatus.Succeeded
                ? ServiceResult<int>.Ok(log.BackupId)
                : ServiceResult<int>.Fail("The backup failed: " + log.Message);
        }

        public IList<BackupRow> History(Actor actor)
        {
            RolePermissions.Demand(actor, Permission.ManageBackups);
            return _db.BackupLogs.AsNoTracking().OrderByDescending(b => b.BackupId).Take(50).Select(b => new BackupRow
            {
                BackupId = b.BackupId, FilePath = b.FilePath, Status = b.Status, StartedAt = b.StartedAt, CompletedAt = b.CompletedAt,
                SizeBytes = b.SizeBytes, RequestedBy = b.RequestedBy.FirstName + " " + b.RequestedBy.LastName, Message = b.Message
            }).ToList();
        }
    }
}
