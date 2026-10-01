<%@ Page Title="My Rentals" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="MyRentals.aspx.cs" Inherits="StudyLink.Web.Student.Books.MyRentals" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="panel">
        <div class="panel__header"><h2 class="panel__title">Rentals</h2><a class="btn btn-sm btn-primary" href="<%: ResolveUrl("~/Catalogue/Books.aspx") %>">Rent a textbook</a></div>
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Book</th><th>Reference</th><th>Rented</th><th>Due</th><th>Status</th><th class="col-num">Renewals</th><th class="col-action"></th></tr></thead>
                <tbody>
                    <asp:Repeater ID="RentalRows" runat="server" OnItemCommand="Rental_Command">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Book"><a href="<%# ResolveUrl("~/Catalogue/BookDetails.aspx?id=" + Eval("BookId")) %>"><%#: Eval("BookTitle") %></a><div class="muted small"><%#: Eval("Campus") %> · <%#: Eval("Barcode") %></div></td>
                                <td data-label="Reference" class="numeric"><%#: Eval("Reference") %></td>
                                <td data-label="Rented"><%# Fmt.Date((DateTime)Eval("RentedAt")) %></td>
                                <td data-label="Due"><%# Fmt.Date((DateTime)Eval("DueDate")) %></td>
                                <td data-label="Status"><%# StatusOf(Container.DataItem) %></td>
                                <td data-label="Renewals" class="col-num"><%# Eval("RenewalCount") %></td>
                                <td class="col-action">
                                    <asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-primary" CommandName="renew" CommandArgument='<%# Eval("RentalId") %>'
                                        Visible='<%# CanRenew(Container.DataItem) %>' data-confirm="Renew this rental? A renewal fee will be added to My Fees.">Renew</asp:LinkButton>
                                    <a class="btn btn-sm btn-accent" href="<%# ResolveUrl("~/Student/Payments/Checkout.aspx?ref=" + Server.UrlEncode((string)Eval("Reference"))) %>" <%# IsPending(Container.DataItem) ? "" : "hidden" %>>Pay</a>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="NoRentals" runat="server" />
    </div>

    <div class="row g-4">
        <div class="col-lg-7">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Purchases</h2></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Purchases" runat="server">
                            <ItemTemplate>
                                <div class="item">
                                    <div class="item__main"><div class="item__title"><%#: Eval("BookTitle") %></div><div class="item__meta"><%#: Eval("Reference") %> · <%# Fmt.Date((DateTime)Eval("CreatedAt")) %></div></div>
                                    <div class="item__side"><span class="fw-semibold"><%# Fmt.Money(Eval("Price")) %></span><%# Fmt.Badge(Eval("Status")) %></div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoPurchases" runat="server" />
                </div>
            </div>
        </div>
        <div class="col-lg-5">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Waitlist</h2></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Waitlist" runat="server" OnItemCommand="Waitlist_Command">
                            <ItemTemplate>
                                <div class="item">
                                    <div class="item__main"><div class="item__title"><%#: Eval("BookTitle") %></div><div class="item__meta"><%#: Eval("Campus") %> · since <%# Fmt.Date((DateTime)Eval("CreatedAt")) %><%# Eval("NotifiedAt") == null ? "" : " · <strong>copy available</strong>" %></div></div>
                                    <div class="item__side"><asp:LinkButton runat="server" CssClass="btn btn-sm btn-link btn-link-danger" CommandName="leave" CommandArgument='<%# Eval("WaitlistId") %>'>Leave</asp:LinkButton></div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoWaitlist" runat="server" />
                </div>
            </div>
        </div>
    </div>
</asp:Content>