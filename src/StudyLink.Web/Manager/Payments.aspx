<%@ Page Title="Payments" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Payments.aspx.cs" Inherits="StudyLink.Web.Manager.Payments" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:PlaceHolder ID="ReceiptPanel" runat="server" Visible="false">
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title">Payment <asp:Literal ID="ReceiptRef" runat="server" /></h2><asp:Literal ID="ReceiptStatus" runat="server" /></div>
            <div class="panel__body">
                <dl class="fact-list mb-3">
                    <dt>Student</dt><dd><asp:Literal ID="ReceiptStudent" runat="server" /></dd>
                    <dt>Date</dt><dd><asp:Literal ID="ReceiptDate" runat="server" /></dd>
                    <dt>Card</dt><dd><asp:Literal ID="ReceiptCard" runat="server" /></dd>
                    <dt>Gateway ref.</dt><dd><asp:Literal ID="ReceiptGateway" runat="server" /></dd>
                    <dt>Amount</dt><dd><asp:Literal ID="ReceiptAmount" runat="server" /></dd>
                </dl>
                <div class="table-wrap">
                    <table class="data-table stack">
                        <thead><tr><th>Item</th><th>Type</th><th class="col-num">Amount</th><th>Status</th><th class="col-action"></th></tr></thead>
                        <tbody>
                            <asp:Repeater ID="Lines" runat="server" OnItemCommand="Line_Command">
                                <ItemTemplate>
                                    <tr>
                                        <td data-label="Item"><%#: Eval("Description") %></td>
                                        <td data-label="Type"><%#: Fmt.Words(Eval("Type")) %></td>
                                        <td data-label="Amount" class="col-num"><%# Fmt.Money(Eval("Amount")) %></td>
                                        <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %></td>
                                        <td class="col-action"><asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-danger" CommandName="refund" CommandArgument='<%# Eval("FeeId") %>'
                                            Visible='<%# CanRefund && Eval("Status").ToString() == "Paid" %>' data-confirm="Refund this item to the student's card?">Refund</asp:LinkButton></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
                <div class="filter-bar mt-3">
                    <div><asp:TextBox ID="RefundReason" runat="server" CssClass="form-control" MaxLength="200" placeholder="Reason for a refund (required, sent to the student)" aria-label="Refund reason" /></div>
                </div>
            </div>
        </div>
    </asp:PlaceHolder>

    <div class="panel">
        <div class="panel__body">
            <asp:Panel runat="server" CssClass="filter-bar" DefaultButton="SearchButton">
                <div><label class="form-label" for="<%= From.ClientID %>">From</label><asp:TextBox ID="From" runat="server" CssClass="form-control" TextMode="Date" /></div>
                <div><label class="form-label" for="<%= To.ClientID %>">To</label><asp:TextBox ID="To" runat="server" CssClass="form-control" TextMode="Date" /></div>
                <div><label class="form-label" for="<%= TypeFilter.ClientID %>">Type</label><asp:DropDownList ID="TypeFilter" runat="server" CssClass="form-select" /></div>
                <div><label class="form-label" for="<%= StatusList.ClientID %>">Status</label><asp:DropDownList ID="StatusList" runat="server" CssClass="form-select" /></div>
                <div><label class="form-label" for="<%= SearchText.ClientID %>">Search</label><asp:TextBox ID="SearchText" runat="server" CssClass="form-control" placeholder="Reference or student" /></div>
                <asp:Button ID="SearchButton" runat="server" CssClass="btn btn-primary" Text="Search" OnClick="Search_Click" />
            </asp:Panel>
        </div>
    </div>
    <asp:Literal ID="Totals" runat="server" />
    <div class="panel">
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Date</th><th>Reference</th><th>Student</th><th>Items</th><th>Card</th><th class="col-num">Amount</th><th>Status</th><th class="col-action"></th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Date"><%# Fmt.DateTime((DateTime)Eval("CreatedAt")) %></td>
                                <td data-label="Reference" class="numeric"><%#: Eval("Reference") %></td>
                                <td data-label="Student"><%#: Eval("StudentName") %><div class="muted small"><%#: Eval("StudentNumber") %></div></td>
                                <td data-label="Items" class="small"><%#: Eval("Items") %></td>
                                <td data-label="Card"><%#: Eval("Card") %></td>
                                <td data-label="Amount" class="col-num"><%# (Eval("Type").ToString() == "Refund" ? "−" : "") + Fmt.Money(Eval("Amount")) %></td>
                                <td data-label="Status"><%# Fmt.Badge(Eval("Type").ToString() == "Refund" ? "Refund" : Eval("Status")) %><div class="muted small"><%#: Eval("FailureReason") %></div></td>
                                <td class="col-action"><a class="btn btn-sm btn-outline-primary" href="<%# Ui.WithQuery(Request, "id", Eval("PaymentId").ToString()) %>">Open</a></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="EmptyState" runat="server" />
        <asp:Literal ID="Pager" runat="server" />
    </div>
</asp:Content>
