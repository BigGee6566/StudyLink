<%@ Page Title="Booking Confirmed" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="BookingConfirmed.aspx.cs" Inherits="StudyLink.Web.Student.Tutoring.BookingConfirmed" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC16 - Booking Confirmed --%>
    <div class="confirm-card doc-watermark">
        <div class="confirm-card__icon"><%= Icons.Svg("check", 38) %></div>
        <h2>Session booked!</h2>
        <p class="muted"><asp:Literal ID="Lead" runat="server" /></p>
        <dl class="fact-list">
            <dt>Reference</dt><dd><asp:Literal ID="Reference" runat="server" /></dd>
            <dt>Tutor</dt><dd><asp:Literal ID="Tutor" runat="server" /></dd>
            <dt>Subject</dt><dd><asp:Literal ID="Subject" runat="server" /></dd>
            <dt>When</dt><dd><asp:Literal ID="When" runat="server" /></dd>
            <dt>Campus</dt><dd><asp:Literal ID="Campus" runat="server" /></dd>
            <dt>Status</dt><dd><asp:Literal ID="Status" runat="server" /></dd>
            <asp:PlaceHolder ID="PaidRow" runat="server"><dt>Paid</dt><dd><asp:Literal ID="Paid" runat="server" /></dd></asp:PlaceHolder>
        </dl>
        <div class="d-grid gap-2">
            <a class="btn btn-primary" href="<%: ResolveUrl("~/Student/Tutoring/MyBookings.aspx") %>">View my bookings</a>
            <a class="btn btn-outline-primary" id="ReceiptLink" runat="server">View receipt</a>
            <a class="btn btn-link" href="<%: ResolveUrl("~/Student/Dashboard.aspx") %>">Back to home</a>
        </div>
    </div>
</asp:Content>