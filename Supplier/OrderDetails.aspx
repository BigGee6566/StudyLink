<%@ Page Title="Order" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="OrderDetails.aspx.cs" Inherits="StudyLink.Web.Supplier.OrderDetails" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <p class="mb-3"><a href="<%: ResolveUrl("~/Supplier/Orders.aspx") %>">&larr; Orders</a></p>
    <asp:PlaceHolder ID="AckPanel" runat="server">
        <div class="alert-box alert-box--info d-flex justify-content-between align-items-center flex-wrap gap-2">
            <div>Please acknowledge this order so the hub knows you are processing it.</div>
            <asp:Button ID="AckButton" runat="server" CssClass="btn btn-primary btn-sm" Text="Acknowledge order" OnClick="Ack_Click" />
        </div>
    </asp:PlaceHolder>
    <div class="panel">
        <div class="panel__header"><h2 class="panel__title">Order lines</h2><asp:Literal ID="StatusBadge" runat="server" /></div>
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Title</th><th>Deliver to</th><th class="col-num">Ordered</th><th class="col-num">Delivered</th><th class="col-num">In transit</th><th class="col-num">Unit cost</th><th class="col-num">Dispatch now</th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Lines" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Title"><%#: Eval("Title") %><div class="muted small"><%#: Eval("Isbn") %></div></td>
                                <td data-label="Deliver to"><%#: Eval("Campus") %></td>
                                <td data-label="Ordered" class="col-num"><%# Eval("QuantityOrdered") %></td>
                                <td data-label="Delivered" class="col-num"><%# Eval("QuantityDelivered") %></td>
                                <td data-label="In transit" class="col-num"><%# Eval("QuantityInTransit") %></td>
                                <td data-label="Unit cost" class="col-num"><%# Fmt.Money(Eval("UnitCost")) %></td>
                                <td data-label="Dispatch now" class="col-num">
                                    <asp:HiddenField ID="LineId" runat="server" Value='<%# Eval("LineId") %>' />
                                    <asp:TextBox ID="Qty" runat="server" CssClass="form-control form-control-sm text-end ms-auto" style="max-width:90px" TextMode="Number"
                                        Text='<%# Eval("Outstanding") %>' Visible='<%# CanDispatch && (int)Eval("Outstanding") > 0 %>' />
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:PlaceHolder ID="DispatchPanel" runat="server">
            <div class="panel__footer">
                <div class="filter-bar">
                    <div><asp:TextBox ID="DispatchNote" runat="server" CssClass="form-control" MaxLength="300" placeholder="Delivery note / waybill number" aria-label="Delivery note" /></div>
                    <asp:Button ID="DispatchButton" runat="server" CssClass="btn btn-primary" Text="Dispatch delivery" OnClick="Dispatch_Click" data-once="Dispatching…" />
                </div>
            </div>
        </asp:PlaceHolder>
    </div>

    <div class="row g-4">
        <div class="col-lg-6">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Deliveries</h2></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Deliveries" runat="server">
                            <ItemTemplate>
                                <div class="item"><div class="item__main"><div class="item__title"><%#: Eval("Reference") %></div><div class="item__meta"><%# Eval("Units") %> units · dispatched <%# Fmt.Date((DateTime)Eval("DispatchedAt")) %><%#: Eval("ReceivedBy") == null ? "" : " · received by " + Eval("ReceivedBy") %></div></div>
                                    <div class="item__side"><%# Fmt.Badge(Eval("Status")) %></div></div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoDeliveries" runat="server" />
                </div>
            </div>
        </div>
        <div class="col-lg-6">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Invoices</h2></div>
                <div class="panel__body">
                    <div class="item-list mb-3">
                        <asp:Repeater ID="InvoiceList" runat="server">
                            <ItemTemplate>
                                <div class="item"><div class="item__main"><div class="item__title"><a href="<%# ResolveUrl("~/Handlers/InvoiceFile.ashx?id=" + Eval("InvoiceId")) %>"><%#: Eval("InvoiceNumber") %></a></div><div class="item__meta"><%# Fmt.Date((DateTime)Eval("UploadedAt")) %><%#: Eval("Note") == null ? "" : " · " + Eval("Note") %></div></div>
                                    <div class="item__side"><span class="numeric fw-semibold"><%# Fmt.Money(Eval("Amount")) %></span><%# Fmt.Badge(Eval("Status")) %></div></div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:PlaceHolder ID="InvoiceForm" runat="server">
                        <h3 class="section-title mt-0">Upload an invoice</h3>
                        <div class="row g-2">
                            <div class="col-6"><label class="form-label" for="<%= InvoiceNumber.ClientID %>">Invoice number</label><asp:TextBox ID="InvoiceNumber" runat="server" CssClass="form-control" MaxLength="40" /></div>
                            <div class="col-6"><label class="form-label" for="<%= Amount.ClientID %>">Amount (R)</label><asp:TextBox ID="Amount" runat="server" CssClass="form-control" inputmode="decimal" /></div>
                            <div class="col-12"><label class="form-label" for="<%= InvoicePdf.ClientID %>">PDF (max 5 MB)</label><asp:FileUpload ID="InvoicePdf" runat="server" CssClass="form-control" accept="application/pdf,.pdf" /></div>
                        </div>
                        <asp:Button ID="UploadButton" runat="server" CssClass="btn btn-outline-primary mt-3" Text="Upload invoice" OnClick="Upload_Click" data-once="Uploading…" />
                    </asp:PlaceHolder>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
