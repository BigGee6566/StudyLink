<%@ Page Title="Supplier Orders" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="SupplierOrders.aspx.cs" Inherits="StudyLink.Web.Manager.SupplierOrders" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC26 - Supplier Orders: create, track, receive (Operations/Deliveries) and review invoices --%>
    <asp:Literal ID="TabStrip" runat="server" />
    <asp:MultiView ID="Views" runat="server" ActiveViewIndex="0">
        <asp:View ID="ListView" runat="server">
            <div class="d-flex justify-content-between flex-wrap gap-2 mb-3">
                <asp:DropDownList ID="StatusFilter" runat="server" CssClass="form-select w-auto" AutoPostBack="true" OnSelectedIndexChanged="StatusFilter_Changed" aria-label="Status" />
                <a class="btn btn-accent" href="<%: ResolveUrl("~/Manager/SupplierOrders.aspx?tab=new") %>">+ New order</a>
            </div>
            <div class="panel">
                <div class="table-wrap">
                    <table class="data-table stack">
                        <thead><tr><th>Order</th><th>Supplier</th><th>Ordered</th><th>Expected</th><th class="col-num">Units</th><th class="col-num">Value</th><th>Status</th><th class="col-action"></th></tr></thead>
                        <tbody>
                            <asp:Repeater ID="OrderRows" runat="server">
                                <ItemTemplate>
                                    <tr>
                                        <td data-label="Order" class="numeric"><%#: Eval("Reference") %><div class="muted small">by <%#: Eval("CreatedBy") %></div></td>
                                        <td data-label="Supplier"><%#: Eval("Supplier") %></td>
                                        <td data-label="Ordered"><%# Fmt.Date((DateTime)Eval("OrderedAt")) %></td>
                                        <td data-label="Expected"><%# Fmt.Date((DateTime?)Eval("ExpectedDate")) %></td>
                                        <td data-label="Units" class="col-num"><%# Eval("UnitsDelivered") %> / <%# Eval("UnitsOrdered") %><%# (int)Eval("PendingDeliveries") > 0 ? "<div class=\"small text-primary\">" + Eval("PendingDeliveries") + " in transit</div>" : "" %></td>
                                        <td data-label="Value" class="col-num"><%# Fmt.Money(Eval("Value")) %></td>
                                        <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %></td>
                                        <td class="col-action"><a class="btn btn-sm btn-outline-primary" href="<%# ResolveUrl("~/Manager/SupplierOrders.aspx?id=" + Eval("OrderId")) %>">Open</a></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
                <asp:Literal ID="NoOrders" runat="server" />
            </div>
        </asp:View>

        <asp:View ID="NewView" runat="server">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">New purchase order</h2></div>
                <div class="panel__body">
                    <div class="row g-3 mb-3">
                        <div class="col-md-4"><label class="form-label" for="<%= NewSupplier.ClientID %>">Supplier</label><asp:DropDownList ID="NewSupplier" runat="server" CssClass="form-select" /></div>
                        <div class="col-md-3"><label class="form-label" for="<%= Expected.ClientID %>">Expected by</label><asp:TextBox ID="Expected" runat="server" CssClass="form-control" TextMode="Date" /></div>
                        <div class="col-md-5"><label class="form-label" for="<%= OrderNotes.ClientID %>">Notes</label><asp:TextBox ID="OrderNotes" runat="server" CssClass="form-control" MaxLength="300" /></div>
                    </div>
                    <div class="table-wrap">
                        <table class="data-table">
                            <thead><tr><th>Title</th><th>Deliver to</th><th class="col-num">Quantity</th><th class="col-num">Unit cost (R)</th></tr></thead>
                            <tbody>
                                <asp:Repeater ID="NewLines" runat="server" OnItemDataBound="NewLines_ItemDataBound">
                                    <ItemTemplate>
                                        <tr>
                                            <td><asp:DropDownList ID="Book" runat="server" CssClass="form-select" aria-label="Title" /></td>
                                            <td><asp:DropDownList ID="LineCampus" runat="server" CssClass="form-select" aria-label="Campus" /></td>
                                            <td><asp:TextBox ID="Qty" runat="server" CssClass="form-control text-end" TextMode="Number" aria-label="Quantity" /></td>
                                            <td><asp:TextBox ID="Cost" runat="server" CssClass="form-control text-end" inputmode="decimal" aria-label="Unit cost" /></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                    <p class="form-hint">Leave unused lines blank. Low-stock titles are listed first.</p>
                    <div class="form-actions"><asp:Button ID="CreateButton" runat="server" CssClass="btn btn-primary" Text="Place order" OnClick="Create_Click" data-once="Placing order…" /></div>
                </div>
            </div>
        </asp:View>

        <asp:View ID="DetailView" runat="server">
            <p class="mb-3"><a href="<%: ResolveUrl("~/Manager/SupplierOrders.aspx") %>">&larr; All orders</a></p>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Order lines</h2><asp:Literal ID="DetailStatus" runat="server" /></div>
                <div class="table-wrap">
                    <table class="data-table stack">
                        <thead><tr><th>Title</th><th>Campus</th><th class="col-num">Ordered</th><th class="col-num">Delivered</th><th class="col-num">In transit</th><th class="col-num">Outstanding</th><th class="col-num">Line total</th></tr></thead>
                        <tbody>
                            <asp:Repeater ID="DetailLines" runat="server">
                                <ItemTemplate>
                                    <tr>
                                        <td data-label="Title"><%#: Eval("Title") %><div class="muted small"><%#: Eval("Isbn") %> · <%# Fmt.Money(Eval("UnitCost")) %> each</div></td>
                                        <td data-label="Campus"><%#: Eval("Campus") %></td>
                                        <td data-label="Ordered" class="col-num"><%# Eval("QuantityOrdered") %></td>
                                        <td data-label="Delivered" class="col-num"><%# Eval("QuantityDelivered") %></td>
                                        <td data-label="In transit" class="col-num"><%# Eval("QuantityInTransit") %></td>
                                        <td data-label="Outstanding" class="col-num"><%# Eval("Outstanding") %></td>
                                        <td data-label="Line total" class="col-num"><%# Fmt.Money(Eval("LineTotal")) %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
                <asp:PlaceHolder ID="CancelPanel" runat="server">
                    <div class="panel__footer">
                        <div class="filter-bar">
                            <div><asp:TextBox ID="CancelReason" runat="server" CssClass="form-control" MaxLength="300" placeholder="Reason for cancelling (sent to the supplier)" aria-label="Cancel reason" /></div>
                            <asp:Button ID="CancelButton" runat="server" CssClass="btn btn-outline-danger" Text="Cancel order" OnClick="Cancel_Click" data-confirm="Cancel this order?" />
                        </div>
                    </div>
                </asp:PlaceHolder>
            </div>
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="panel">
                        <div class="panel__header"><h2 class="panel__title">Deliveries</h2><a href="<%: ResolveUrl("~/Operations/Deliveries.aspx") %>">Receive deliveries</a></div>
                        <div class="panel__body">
                            <div class="item-list">
                                <asp:Repeater ID="DetailDeliveries" runat="server">
                                    <ItemTemplate>
                                        <div class="item"><div class="item__main"><div class="item__title"><%#: Eval("Reference") %></div><div class="item__meta"><%# Eval("Units") %> units · <%# Fmt.Date((DateTime)Eval("DispatchedAt")) %><%#: Eval("ReceivedBy") == null ? "" : " · received by " + Eval("ReceivedBy") %></div></div>
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
                            <div class="item-list">
                                <asp:Repeater ID="DetailInvoices" runat="server">
                                    <ItemTemplate>
                                        <div class="item"><div class="item__main"><div class="item__title"><a href="<%# ResolveUrl("~/Handlers/InvoiceFile.ashx?id=" + Eval("InvoiceId")) %>"><%#: Eval("InvoiceNumber") %></a></div><div class="item__meta"><%# Fmt.Date((DateTime)Eval("UploadedAt")) %></div></div>
                                            <div class="item__side"><span class="numeric fw-semibold"><%# Fmt.Money(Eval("Amount")) %></span><%# Fmt.Badge(Eval("Status")) %></div></div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                            <asp:Literal ID="NoInvoices" runat="server" />
                            <p class="form-hint mt-2 mb-0">Review invoices on the <a href="<%: ResolveUrl("~/Manager/SupplierOrders.aspx?tab=invoices") %>">Invoices</a> tab.</p>
                        </div>
                    </div>
                </div>
            </div>
        </asp:View>

        <asp:View ID="InvoicesView" runat="server">
            <div class="panel">
                <div class="table-wrap">
                    <table class="data-table stack">
                        <thead><tr><th>Invoice</th><th>Supplier</th><th>Order</th><th>Uploaded</th><th class="col-num">Amount</th><th>Status</th><th class="col-action"></th></tr></thead>
                        <tbody>
                            <asp:Repeater ID="InvoiceRows" runat="server" OnItemCommand="Invoice_Command">
                                <ItemTemplate>
                                    <tr>
                                        <td data-label="Invoice"><a href="<%# ResolveUrl("~/Handlers/InvoiceFile.ashx?id=" + Eval("InvoiceId")) %>"><%#: Eval("InvoiceNumber") %></a></td>
                                        <td data-label="Supplier"><%#: Eval("Supplier") %></td>
                                        <td data-label="Order"><a href="<%# ResolveUrl("~/Manager/SupplierOrders.aspx?id=" + Eval("OrderId")) %>"><%#: Eval("OrderReference") %></a></td>
                                        <td data-label="Uploaded"><%# Fmt.Date((DateTime)Eval("UploadedAt")) %></td>
                                        <td data-label="Amount" class="col-num"><%# Fmt.Money(Eval("Amount")) %></td>
                                        <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %><div class="muted small"><%#: Eval("Note") %></div></td>
                                        <td class="col-action">
                                            <asp:LinkButton runat="server" CssClass="btn btn-sm btn-primary" CommandName="Approved" CommandArgument='<%# Eval("InvoiceId") %>' Visible='<%# Eval("Status").ToString() == "Submitted" %>'>Approve</asp:LinkButton>
                                            <asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-danger" CommandName="Rejected" CommandArgument='<%# Eval("InvoiceId") %>' Visible='<%# Eval("Status").ToString() == "Submitted" %>'>Reject</asp:LinkButton>
                                            <asp:LinkButton runat="server" CssClass="btn btn-sm btn-accent" CommandName="Paid" CommandArgument='<%# Eval("InvoiceId") %>' Visible='<%# Eval("Status").ToString() == "Approved" %>' data-confirm="Mark this invoice as paid?">Mark paid</asp:LinkButton>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
                <asp:Literal ID="NoInvoiceRows" runat="server" />
                <div class="panel__footer">
                    <asp:TextBox ID="InvoiceNote" runat="server" CssClass="form-control" MaxLength="300" placeholder="Note to the supplier (required when rejecting)" aria-label="Invoice note" />
                </div>
            </div>
        </asp:View>
    </asp:MultiView>
</asp:Content>
