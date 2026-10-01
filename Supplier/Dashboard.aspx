<%@ Page Title="Home" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="StudyLink.Web.Supplier.Dashboard" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC18 - Supplier Dashboard --%>
    <asp:Literal ID="Kpis" runat="server" />
    <div class="row g-4">
        <div class="col-lg-7">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Open orders</h2><a href="<%: ResolveUrl("~/Supplier/Orders.aspx") %>">All orders</a></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Orders" runat="server">
                            <ItemTemplate>
                                <a class="item" href="<%# ResolveUrl("~/Supplier/OrderDetails.aspx?id=" + Eval("OrderId")) %>">
                                    <div class="item__main"><div class="item__title"><%#: Eval("Reference") %></div>
                                        <div class="item__meta"><%# Eval("UnitsDelivered") %> of <%# Eval("UnitsOrdered") %> units delivered · ordered <%# Fmt.Date((DateTime)Eval("OrderedAt")) %><%# Eval("ExpectedDate") == null ? "" : " · expected " + Fmt.Date((DateTime?)Eval("ExpectedDate")) %></div></div>
                                    <div class="item__side"><span class="fw-semibold numeric"><%# Fmt.Money(Eval("Value")) %></span><%# Fmt.Badge(Eval("Status")) %></div>
                                </a>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoOrders" runat="server" />
                </div>
            </div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Recent invoices</h2><a href="<%: ResolveUrl("~/Supplier/Invoices.aspx") %>">All invoices</a></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Invoices" runat="server">
                            <ItemTemplate>
                                <div class="item"><div class="item__main"><div class="item__title"><%#: Eval("InvoiceNumber") %></div><div class="item__meta">Order <%#: Eval("OrderReference") %> · <%# Fmt.Date((DateTime)Eval("UploadedAt")) %></div></div>
                                    <div class="item__side"><span class="fw-semibold numeric"><%# Fmt.Money(Eval("Amount")) %></span><%# Fmt.Badge(Eval("Status")) %></div></div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoInvoices" runat="server" />
                </div>
            </div>
        </div>
        <div class="col-lg-5">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Your titles running low</h2><a href="<%: ResolveUrl("~/Supplier/Stock.aspx") %>">Stock</a></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="LowStock" runat="server">
                            <ItemTemplate>
                                <div class="item"><div class="item__main"><div class="item__title"><%#: Eval("Title") %></div><div class="item__meta"><%#: Eval("Isbn") %> · reorder at <%# Eval("ReorderLevel") %></div></div>
                                    <div class="item__side"><span class="badge badge--pending"><%# Eval("Available") %> available</span><span class="muted small"><%# Eval("OnOrder") %> on order</span></div></div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoLowStock" runat="server" />
                </div>
            </div>
        </div>
    </div>
</asp:Content>
