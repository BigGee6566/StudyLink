<%@ Page Title="Deliveries" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Deliveries.aspx.cs" Inherits="StudyLink.Web.Supplier.Deliveries" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="panel">
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Delivery</th><th>Order</th><th>Dispatched</th><th class="col-num">Units</th><th>Received</th><th>Status</th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Delivery" class="numeric"><%#: Eval("Reference") %><div class="muted small"><%#: Eval("Note") %></div></td>
                                <td data-label="Order"><a href="<%# ResolveUrl("~/Supplier/OrderDetails.aspx?id=" + Eval("OrderId")) %>"><%#: Eval("OrderReference") %></a></td>
                                <td data-label="Dispatched"><%# Fmt.DateTime((DateTime)Eval("DispatchedAt")) %></td>
                                <td data-label="Units" class="col-num"><%# Eval("Units") %></td>
                                <td data-label="Received"><%# Fmt.DateTime((DateTime?)Eval("ReceivedAt")) %><div class="muted small"><%#: Eval("ReceivedBy") %></div></td>
                                <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="EmptyState" runat="server" />
    </div>
</asp:Content>
