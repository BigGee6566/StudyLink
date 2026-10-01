<%@ Page Title="Book details" Language="C#" AutoEventWireup="true" CodeBehind="BookDetails.aspx.cs" Inherits="StudyLink.Web.Catalogue.BookDetails" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC12 - Book Details: rental terms, live stock per campus, rent / buy / waitlist --%>
    <p class="mb-3"><a href="<%: ResolveUrl("~/Catalogue/Books.aspx") %>">&larr; Back to textbooks</a></p>
    <div class="panel">
        <div class="panel__body">
            <div class="book-hero">
                <div class="book-cover">
                    <asp:Literal ID="Cover" runat="server" />
                    <span><asp:Literal ID="CoverTitle" runat="server" /></span>
                </div>
                <div>
                    <h2 class="h3 fw-bold mb-1"><asp:Literal ID="BookTitle" runat="server" /></h2>
                    <p class="muted mb-3"><asp:Literal ID="Byline" runat="server" /></p>
                    <dl class="fact-list mb-3">
                        <dt>ISBN</dt><dd><asp:Literal ID="Isbn" runat="server" /></dd>
                        <dt>Category</dt><dd><asp:Literal ID="CategoryName" runat="server" /></dd>
                        <dt>Subject</dt><dd><asp:Literal ID="SubjectName" runat="server" /></dd>
                        <dt>Publisher</dt><dd><asp:Literal ID="Publisher" runat="server" /></dd>
                    </dl>
                    <p><asp:Literal ID="Description" runat="server" /></p>
                </div>
            </div>
        </div>
    </div>

    <div class="detail-grid">
        <div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Availability by campus</h2></div>
                <div class="panel__body">
                    <div class="mb-3" style="max-width:320px">
                        <label class="form-label" for="<%= Campus.ClientID %>">Collect at</label>
                        <asp:DropDownList ID="Campus" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="Campus_Changed" />
                    </div>
                    <div class="item-list">
                        <asp:Repeater ID="StockList" runat="server">
                            <ItemTemplate>
                                <div class="item">
                                    <div class="item__main"><div class="item__title"><%#: Eval("Campus") %> campus</div></div>
                                    <div class="item__side"><%# StockBadge((int)Eval("Available")) %></div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </div>
        </div>
        <div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Rental terms</h2></div>
                <div class="panel__body">
                    <div class="summary-box mb-3">
                        <dl class="fact-list">
                            <asp:PlaceHolder ID="RentTerms" runat="server">
                                <dt>Rental fee</dt><dd><asp:Literal ID="RentalFee" runat="server" /></dd>
                                <dt>Rental period</dt><dd><asp:Literal ID="Period" runat="server" /></dd>
                                <dt>Due back if rented today</dt><dd><asp:Literal ID="DueDate" runat="server" /></dd>
                                <dt>Late fee</dt><dd><asp:Literal ID="LateFee" runat="server" /></dd>
                            </asp:PlaceHolder>
                            <asp:PlaceHolder ID="SaleTerms" runat="server">
                                <dt>Buy price</dt><dd><asp:Literal ID="SalePrice" runat="server" /></dd>
                            </asp:PlaceHolder>
                        </dl>
                    </div>
                    <asp:Literal ID="AvailabilityNote" runat="server" />
                    <asp:PlaceHolder ID="StudentActions" runat="server">
                        <div class="d-grid gap-2">
                            <asp:Button ID="RentButton" runat="server" CssClass="btn btn-primary btn-lg" Text="Rent this book" OnClick="Rent_Click" data-once="Reserving…" />
                            <asp:Button ID="BuyButton" runat="server" CssClass="btn btn-outline-primary" Text="Buy this book" OnClick="Buy_Click" data-once="Reserving…" />
                            <asp:Button ID="WaitlistButton" runat="server" CssClass="btn btn-outline-primary" Text="Join the waitlist" OnClick="Waitlist_Click" />
                        </div>
                        <p class="form-hint mt-2">A copy is held for you while you pay. Unpaid holds are released automatically.</p>
                    </asp:PlaceHolder>
                    <asp:PlaceHolder ID="VisitorActions" runat="server">
                        <a class="btn btn-primary btn-block" href="<%: LoginUrl %>">Log in to rent or buy</a>
                        <p class="form-hint mt-2 text-center">New here? <a href="<%: ResolveUrl("~/Account/Register.aspx?role=student") %>">Register as a student</a>.</p>
                    </asp:PlaceHolder>
                </div>
            </div>
        </div>
    </div>
</asp:Content>