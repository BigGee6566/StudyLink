<%@ Page Title="My Fees" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="MyFees.aspx.cs" Inherits="StudyLink.Web.Student.Fees.MyFees" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC14 - My Fees --%>
    <asp:Literal ID="Balance" runat="server" />
    <div class="panel">
        <div class="panel__header"><h2 class="panel__title">Unpaid items</h2><asp:HyperLink ID="PayAll" runat="server" CssClass="btn btn-sm btn-accent">Pay fees</asp:HyperLink></div>
        <div class="panel__body">
            <div class="item-list">
                <asp:Repeater ID="Unpaid" runat="server">
                    <ItemTemplate>
                        <div class="item">
                            <div class="item__main">
                                <div class="item__title"><%#: Eval("Description") %></div>
                                <div class="item__meta"><%#: Fmt.Words(Eval("Type")) %> · <%# Fmt.Date((DateTime)Eval("CreatedAt")) %><%# Eval("PayBy") == null ? "" : " · <strong>pay by " + Fmt.DateTime((DateTime?)Eval("PayBy")) + "</strong>" %></div>
                            </div>
                            <div class="item__side"><span class="fw-semibold numeric"><%# Fmt.Money(Eval("Amount")) %></span>
                                <a class="small" href="<%# ResolveUrl("~/Student/Fees/PayFees.aspx?fee=" + Eval("FeeId")) %>">Pay this</a></div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
            <asp:Literal ID="NoUnpaid" runat="server" />
        </div>
    </div>

    <div class="panel">
        <div class="panel__header"><h2 class="panel__title">Payments &amp; receipts</h2><span class="muted small">Paid this year: <asp:Literal ID="PaidYear" runat="server" /></span></div>
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Date</th><th>Reference</th><th>Items</th><th>Card</th><th class="col-num">Amount</th><th>Status</th><th class="col-action"></th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Payments" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Date"><%# Fmt.DateTime((DateTime)Eval("CreatedAt")) %></td>
                                <td data-label="Reference" class="numeric"><%#: Eval("Reference") %></td>
                                <td data-label="Items"><%#: Eval("Items") %></td>
                                <td data-label="Card"><%#: Eval("Card") %></td>
                                <td data-label="Amount" class="col-num"><%# (Eval("Type").ToString() == "Refund" ? "−" : "") + Fmt.Money(Eval("Amount")) %></td>
                                <td data-label="Status"><%# Fmt.Badge(Eval("Type").ToString() == "Refund" ? "Refund" : Eval("Status")) %></td>
                                <td class="col-action"><a class="btn btn-sm btn-outline-primary" href="<%# ResolveUrl("~/Student/Fees/PaymentComplete.aspx?id=" + Eval("PaymentId")) %>">Receipt</a></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="NoPayments" runat="server" />
    </div>

    <div class="panel">
        <div class="panel__header"><h2 class="panel__title">Fee history</h2></div>
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Item</th><th>Type</th><th>Raised</th><th>Settled</th><th class="col-num">Amount</th><th>Status</th></tr></thead>
                <tbody>
                    <asp:Repeater ID="History" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Item"><%#: Eval("Description") %></td>
                                <td data-label="Type"><%#: Fmt.Words(Eval("Type")) %></td>
                                <td data-label="Raised"><%# Fmt.Date((DateTime)Eval("CreatedAt")) %></td>
                                <td data-label="Settled"><%# Fmt.Date((DateTime?)Eval("SettledAt")) %></td>
                                <td data-label="Amount" class="col-num"><%# Fmt.Money(Eval("Amount")) %></td>
                                <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="NoHistory" runat="server" />
    </div>
</asp:Content>