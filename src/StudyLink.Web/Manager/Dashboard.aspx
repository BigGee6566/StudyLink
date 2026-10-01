<%@ Page Title="Home" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="StudyLink.Web.Manager.Dashboard" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC20 - Manager Dashboard --%>
    <asp:Literal ID="Kpis" runat="server" />
    <div class="row g-4">
        <div class="col-lg-7">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Revenue, last 14 days</h2><a href="<%: ResolveUrl("~/Operations/Reports.aspx?key=revenue") %>">Revenue report</a></div>
                <div class="panel__body"><asp:Literal ID="Trend" runat="server" /></div>
            </div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Recent payments</h2><a href="<%: ResolveUrl("~/Manager/Payments.aspx") %>">All payments</a></div>
                <div class="table-wrap">
                    <table class="data-table stack">
                        <thead><tr><th>When</th><th>Student</th><th>Items</th><th class="col-num">Amount</th></tr></thead>
                        <tbody>
                            <asp:Repeater ID="Payments" runat="server">
                                <ItemTemplate>
                                    <tr>
                                        <td data-label="When"><%# Fmt.Ago((DateTime)Eval("CreatedAt")) %></td>
                                        <td data-label="Student"><%#: Eval("StudentName") %></td>
                                        <td data-label="Items" class="small"><%#: Eval("Items") %></td>
                                        <td data-label="Amount" class="col-num"><%# (Eval("Type").ToString() == "Refund" ? "−" : "") + Fmt.Money(Eval("Amount")) %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
        <div class="col-lg-5">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Low stock</h2><a href="<%: ResolveUrl("~/Manager/Books.aspx?view=inventory&low=1") %>">Inventory</a></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="LowStock" runat="server">
                            <ItemTemplate>
                                <a class="item" href="<%# ResolveUrl("~/Manager/BookEdit.aspx?id=" + Eval("BookId")) %>">
                                    <div class="item__main"><div class="item__title"><%#: Eval("Title") %></div><div class="item__meta">Reorder at <%# Eval("Stock.ReorderLevel") %><%#: Eval("PreferredSupplier") == null ? "" : " · " + Eval("PreferredSupplier") %></div></div>
                                    <div class="item__side"><span class="badge badge--<%# (int)Eval("Stock.Available") == 0 ? "danger" : "pending" %>"><%# Eval("Stock.Available") %> available</span></div>
                                </a>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoLowStock" runat="server" />
                </div>
            </div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Needs attention</h2></div>
                <div class="panel__body">
                    <div class="action-list">
                        <a class="action-card" href="<%: ResolveUrl("~/Operations/TutorVerification.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("shield") %></span><span><span class="action-card__title d-block">Tutor verification</span><span class="action-card__text"><asp:Literal ID="PendingTutors" runat="server" /></span></span></a>
                        <a class="action-card" href="<%: ResolveUrl("~/Operations/Deliveries.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("truck") %></span><span><span class="action-card__title d-block">Deliveries to receive</span><span class="action-card__text"><asp:Literal ID="DeliveriesText" runat="server" /></span></span></a>
                        <a class="action-card" href="<%: ResolveUrl("~/Operations/Returns.aspx?view=overdue") %>"><span class="action-card__icon"><%= Icons.Svg("alert") %></span><span><span class="action-card__title d-block">Overdue books</span><span class="action-card__text"><asp:Literal ID="OverdueText" runat="server" /></span></span></a>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
