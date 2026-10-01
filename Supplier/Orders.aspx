<%@ Page Title="Orders" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Orders.aspx.cs" Inherits="StudyLink.Web.Supplier.Orders" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:Literal ID="TabStrip" runat="server" />
    <div class="panel">
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Order</th><th>Ordered</th><th>Expected</th><th class="col-num">Units</th><th class="col-num">Value</th><th>Status</th><th class="col-action"></th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Order" class="numeric"><%#: Eval("Reference") %><div class="muted small"><%# Eval("Lines") %> lines</div></td>
                                <td data-label="Ordered"><%# Fmt.Date((DateTime)Eval("OrderedAt")) %></td>
                                <td data-label="Expected"><%# Fmt.Date((DateTime?)Eval("ExpectedDate")) %></td>
                                <td data-label="Units" class="col-num"><%# Eval("UnitsDelivered") %> / <%# Eval("UnitsOrdered") %></td>
                                <td data-label="Value" class="col-num"><%# Fmt.Money(Eval("Value")) %></td>
                                <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %></td>
                                <td class="col-action"><a class="btn btn-sm btn-outline-primary" href="<%# ResolveUrl("~/Supplier/OrderDetails.aspx?id=" + Eval("OrderId")) %>">Open</a></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="EmptyState" runat="server" />
    </div>
</asp:Content>
