<%@ Page Title="Home" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="StudyLink.Web.Tutor.Dashboard" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC10 - Tutor Dashboard: sessions today, utilisation, requests, timesheet week --%>
    <asp:Literal ID="StatusNote" runat="server" />
    <asp:Literal ID="Kpis" runat="server" />
    <div class="row g-4">
        <div class="col-lg-7">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Today's sessions</h2><a href="<%: ResolveUrl("~/Tutor/Schedule.aspx") %>">Full schedule</a></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Today" runat="server">
                            <ItemTemplate>
                                <a class="item" href="<%# ResolveUrl("~/Tutor/SessionDetails.aspx?id=" + Eval("BookingId")) %>">
                                    <div class="avatar" aria-hidden="true"><%# Ui.Initials((string)Eval("StudentName")) %></div>
                                    <div class="item__main"><div class="item__title"><%#: Eval("StudentName") %></div><div class="item__meta"><%#: Eval("TimeRange") %> · <%#: Eval("Subject") %> · <%#: Eval("Campus") %></div></div>
                                    <div class="item__side"><%# Fmt.Badge(Eval("Status")) %></div>
                                </a>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoToday" runat="server" />
                </div>
            </div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Coming up</h2></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Upcoming" runat="server">
                            <ItemTemplate>
                                <a class="item" href="<%# ResolveUrl("~/Tutor/SessionDetails.aspx?id=" + Eval("BookingId")) %>">
                                    <div class="item__main"><div class="item__title"><%#: Eval("Subject") %> · <%#: Eval("StudentName") %></div><div class="item__meta"><%# Fmt.Day((DateTime)Eval("SessionDate")) %> · <%#: Eval("TimeRange") %></div></div>
                                    <div class="item__side"><%# Fmt.Badge(Eval("Status")) %></div>
                                </a>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoUpcoming" runat="server" />
                </div>
            </div>
        </div>
        <div class="col-lg-5">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">This week's timesheet</h2></div>
                <div class="panel__body">
                    <dl class="fact-list mb-3">
                        <dt>Week of</dt><dd><asp:Literal ID="WeekOf" runat="server" /></dd>
                        <dt>Hours logged</dt><dd><asp:Literal ID="WeekHours" runat="server" /></dd>
                        <dt>Status</dt><dd><asp:Literal ID="WeekStatus" runat="server" /></dd>
                    </dl>
                    <a class="btn btn-outline-primary btn-block" href="<%: ResolveUrl("~/Tutor/Timesheet.aspx") %>">Open timesheet</a>
                </div>
            </div>
            <div class="action-list">
                <a class="action-card" href="<%: ResolveUrl("~/Tutor/Availability.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("clock") %></span><span><span class="action-card__title d-block">Publish availability</span><span class="action-card__text"><asp:Literal ID="OpenSlots" runat="server" /></span></span></a>
                <a class="action-card" href="<%: ResolveUrl("~/Tutor/Subjects.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("cap") %></span><span><span class="action-card__title d-block">My subjects</span><span class="action-card__text">Subjects and levels you tutor</span></span></a>
                <a class="action-card" href="<%: ResolveUrl("~/Tutor/Reviews.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("star") %></span><span><span class="action-card__title d-block">My reviews</span><span class="action-card__text"><asp:Literal ID="RatingText" runat="server" /></span></span></a>
            </div>
        </div>
    </div>
</asp:Content>