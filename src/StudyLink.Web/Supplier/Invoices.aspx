<%@ Page Title="Invoices" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Invoices.aspx.cs" Inherits="StudyLink.Web.Supplier.Invoices" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:Literal ID="Kpis" runat="server" />
    <div class="panel">
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Invoice</th><th>Order</th><th>Uploaded</th><th class="col-num">Amount</th><th>Status</th><th>Paid</th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Invoice"><a href="<%# ResolveUrl("~/Handlers/InvoiceFile.ashx?id=" + Eval("InvoiceId")) %>"><%#: Eval("InvoiceNumber") %></a></td>
                                <td data-label="Order"><a href="<%# ResolveUrl("~/Supplier/OrderDetails.aspx?id=" + Eval("OrderId")) %>"><%#: Eval("OrderReference") %></a></td>
                                <td data-label="Uploaded"><%# Fmt.Date((DateTime)Eval("UploadedAt")) %></td>
                                <td data-label="Amount" class="col-num"><%# Fmt.Money(Eval("Amount")) %></td>
                                <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %><div class="muted small"><%#: Eval("Note") %></div></td>
                                <td data-label="Paid"><%# Fmt.Date((DateTime?)Eval("PaidAt")) %></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="EmptyState" runat="server" />
    </div>
    <p class="muted small">Upload invoices from the order they belong to.</p>
</asp:Content>
