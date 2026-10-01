<%@ Page Title="Textbook Rentals" Language="C#" AutoEventWireup="true" CodeBehind="Books.aspx.cs" Inherits="StudyLink.Web.Catalogue.Books" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC11 - Textbook Rentals (browse, search, live stock per campus) --%>
    <div class="panel">
        <div class="panel__body">
            <asp:Panel runat="server" CssClass="filter-bar" DefaultButton="SearchButton">
                <div>
                    <label class="form-label" for="<%= SearchText.ClientID %>">Search</label>
                    <asp:TextBox ID="SearchText" runat="server" CssClass="form-control" MaxLength="100" placeholder="Title, author or ISBN" />
                </div>
                <div>
                    <label class="form-label" for="<%= Category.ClientID %>">Category</label>
                    <asp:DropDownList ID="Category" runat="server" CssClass="form-select" />
                </div>
                <div>
                    <label class="form-label" for="<%= Campus.ClientID %>">Campus</label>
                    <asp:DropDownList ID="Campus" runat="server" CssClass="form-select" />
                </div>
                <div class="filter-actions d-flex align-items-center gap-3">
                    <div class="form-check mb-0">
                        <asp:CheckBox ID="AvailableOnly" runat="server" />
                        <label class="form-check-label" for="<%= AvailableOnly.ClientID %>">In stock only</label>
                    </div>
                    <asp:Button ID="SearchButton" runat="server" CssClass="btn btn-primary" Text="Search" OnClick="Search_Click" />
                </div>
            </asp:Panel>
        </div>
    </div>

    <p class="muted"><asp:Literal ID="ResultSummary" runat="server" /></p>
    <div class="book-grid">
        <asp:Repeater ID="BookList" runat="server">
            <ItemTemplate>
                <a class="book-card" href="<%# ResolveUrl("~/Catalogue/BookDetails.aspx?id=" + Eval("BookId") + CampusQuery) %>">
                    <div class="book-cover">
                        <%# (bool)Eval("HasCover") ? "<img src=\"" + ResolveUrl("~/Handlers/BookCover.ashx?id=" + Eval("BookId")) + "\" alt=\"\" loading=\"lazy\" />" : "" %>
                        <span><%#: Eval("Title") %></span>
                    </div>
                    <div class="book-card__body">
                        <div class="book-card__title"><%#: Eval("Title") %></div>
                        <div class="book-card__meta"><%#: Eval("Authors") %><%#: string.IsNullOrEmpty((string)Eval("Edition")) ? "" : " · " + Eval("Edition") %></div>
                        <div class="book-card__meta"><%#: Eval("Category") %></div>
                        <div class="book-card__foot">
                            <span class="fw-semibold"><%# (bool)Eval("IsRentable") ? Fmt.Money(Eval("WeeklyRentalFee")) + "<span class=\"muted fw-normal\">/week</span>" : Fmt.Money(Eval("SalePrice")) %></span>
                            <%# StockBadge((int)Eval("Available")) %>
                        </div>
                    </div>
                </a>
            </ItemTemplate>
        </asp:Repeater>
    </div>
    <asp:Literal ID="EmptyState" runat="server" />
    <asp:Literal ID="Pager" runat="server" />
</asp:Content>
