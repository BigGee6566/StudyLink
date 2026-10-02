<%@ Page Title="Home" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="StudyLink.Web.Student.Dashboard" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC6 - Student Dashboard --%>
    <asp:Literal ID="Reminder" runat="server" />
    <asp:Literal ID="Balance" runat="server" />

    <div class="action-list mb-4" style="grid-template-columns:repeat(auto-fit,minmax(240px,1fr))">
        <a class="action-card" href="<%: ResolveUrl("~/Catalogue/Tutors.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("cap") %></span><span><span class="action-card__title d-block">Book a tutoring session</span><span class="action-card__text">Verified tutors at your campus</span></span><span class="action-card__chev"><%= Icons.Svg("arrow") %></span></a>
        <a class="action-card" href="<%: ResolveUrl("~/Catalogue/Books.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("book") %></span><span><span class="action-card__title d-block">Rent or buy a textbook</span><span class="action-card__text">Live stock per campus</span></span><span class="action-card__chev"><%= Icons.Svg("arrow") %></span></a>
        <a class="action-card" href="<%: ResolveUrl("~/Student/Fees/MyFees.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("card") %></span><span><span class="action-card__title d-block">My fees &amp; payments</span><span class="action-card__text">Statement and receipts</span></span><span class="action-card__chev"><%= Icons.Svg("arrow") %></span></a>
    </div>

    <div class="row g-4">
        <div class="col-lg-6">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Upcoming sessions</h2><a href="<%: ResolveUrl("~/Student/Tutoring/MyBookings.aspx") %>">View all</a></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Sessions" runat="server">
                            <ItemTemplate>
                                <a class="item" href="<%# ResolveUrl("~/Student/Tutoring/BookingDetails.aspx?id=" + Eval("BookingId")) %>">
                                    <%# Icons.Subject((string)Eval("Subject"), 18) %>
                                    <div class="item__main">
                                        <div class="item__title"><%#: Eval("Subject") %> with <%#: Eval("TutorName") %></div>
                                        <div class="item__meta"><%# Fmt.Day((DateTime)Eval("SessionDate")) %> · <%#: Eval("TimeRange") %> · <%#: Eval("Campus") %></div>
                                    </div>
                                    <div class="item__side"><%# Fmt.Badge(Eval("Status")) %></div>
                                </a>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoSessions" runat="server" />
                </div>
            </div>
        </div>
        <div class="col-lg-6">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Books on loan</h2><a href="<%: ResolveUrl("~/Student/Books/MyRentals.aspx") %>">View all</a></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Rentals" runat="server">
                            <ItemTemplate>
                                <div class="item">
                                    <div class="item__main">
                                        <div class="item__title"><%#: Eval("BookTitle") %></div>
                                        <div class="item__meta">Due <%# Fmt.Date((DateTime)Eval("DueDate")) %> · <%#: Eval("Campus") %></div>
                                    </div>
                                    <div class="item__side"><%# (bool)Eval("IsOverdue") ? "<span class=\"badge badge--danger\">" + Eval("DaysOverdue") + " days overdue</span>" : Fmt.Badge(Eval("Status")) %></div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoRentals" runat="server" />
                </div>
            </div>
        </div>
    </div>
</asp:Content>