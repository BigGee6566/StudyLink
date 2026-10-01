<%@ Page Title="Payment Complete" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="PaymentComplete.aspx.cs" Inherits="StudyLink.Web.Student.Fees.PaymentComplete" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC17 - Payment Complete / printable receipt --%>
    <div class="confirm-card">
        <div class="confirm-card__icon"><%= Icons.Svg("check", 38) %></div>
        <h2><asp:Literal ID="Headline" runat="server" /></h2>
        <p class="muted mb-1"><asp:Literal ID="Organisation" runat="server" /></p>
        <p class="fs-2 fw-bold numeric mb-0"><asp:Literal ID="Amount" runat="server" /></p>
        <dl class="fact-list">
            <dt>Receipt no.</dt><dd><asp:Literal ID="Reference" runat="server" /></dd>
            <dt>Date</dt><dd><asp:Literal ID="Date" runat="server" /></dd>
            <dt>Student</dt><dd><asp:Literal ID="Student" runat="server" /></dd>
            <dt>Card</dt><dd><asp:Literal ID="Card" runat="server" /></dd>
            <dt>Gateway ref.</dt><dd><asp:Literal ID="Gateway" runat="server" /></dd>
        </dl>
        <div class="summary-box text-start mb-3">
            <asp:Repeater ID="Lines" runat="server">
                <ItemTemplate>
                    <div class="d-flex justify-content-between gap-3 mb-2"><span><%#: Eval("Description") %></span><span class="numeric text-nowrap"><%# Fmt.Money(Eval("Amount")) %></span></div>
                </ItemTemplate>
            </asp:Repeater>
            <div class="summary-total"><span>Total</span><span class="numeric"><asp:Literal ID="Total" runat="server" /></span></div>
            <div class="d-flex justify-content-between small muted mt-2"><span>Balance still outstanding</span><span class="numeric"><asp:Literal ID="OutstandingAfter" runat="server" /></span></div>
        </div>
        <div class="d-grid gap-2 no-print">
            <button type="button" class="btn btn-outline-primary" onclick="window.print()">Print receipt</button>
            <a class="btn btn-primary" href="<%: ResolveUrl("~/Student/Fees/MyFees.aspx") %>">Back to My Fees</a>
        </div>
    </div>
</asp:Content>