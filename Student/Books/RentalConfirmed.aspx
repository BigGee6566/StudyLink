<%@ Page Title="Rental Confirmed" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="RentalConfirmed.aspx.cs" Inherits="StudyLink.Web.Student.Books.RentalConfirmed" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC13 - Rental Confirmed --%>
    <div class="confirm-card doc-watermark">
        <div class="confirm-card__icon"><%= Icons.Svg("check", 38) %></div>
        <h2><asp:Literal ID="Headline" runat="server" /></h2>
        <p class="muted"><asp:Literal ID="Lead" runat="server" /></p>
        <dl class="fact-list">
            <dt>Book</dt><dd><asp:Literal ID="BookTitle" runat="server" /></dd>
            <dt>Reference</dt><dd><asp:Literal ID="Reference" runat="server" /></dd>
            <dt>Copy</dt><dd><asp:Literal ID="Barcode" runat="server" /></dd>
            <asp:PlaceHolder ID="RentalRows" runat="server">
                <dt>Collect at</dt><dd><asp:Literal ID="Campus" runat="server" /></dd>
                <dt>Due back</dt><dd><asp:Literal ID="DueDate" runat="server" /></dd>
                <dt>Late fee</dt><dd><asp:Literal ID="LateFee" runat="server" /></dd>
            </asp:PlaceHolder>
            <asp:PlaceHolder ID="PaidRow" runat="server"><dt>Paid</dt><dd><asp:Literal ID="Paid" runat="server" /></dd></asp:PlaceHolder>
        </dl>
        <div class="d-grid gap-2">
            <a class="btn btn-primary" href="<%: ResolveUrl("~/Student/Books/MyRentals.aspx") %>">View my rentals</a>
            <a class="btn btn-outline-primary" id="ReceiptLink" runat="server">View receipt</a>
            <a class="btn btn-link" href="<%: ResolveUrl("~/Catalogue/Books.aspx") %>">Browse more textbooks</a>
        </div>
    </div>
</asp:Content>