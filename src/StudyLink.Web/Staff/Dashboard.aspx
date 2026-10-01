<%@ Page Title="Home" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="StudyLink.Web.Staff.Dashboard" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC19 - Staff Dashboard --%>
    <asp:Literal ID="Kpis" runat="server" />
    <div class="row g-4">
        <div class="col-lg-6">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Tutors awaiting verification</h2><a href="<%: ResolveUrl("~/Operations/TutorVerification.aspx") %>">Open queue</a></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Tutors" runat="server">
                            <ItemTemplate>
                                <a class="item" href="<%# ResolveUrl("~/Operations/TutorVerification.aspx?id=" + Eval("TutorId")) %>">
                                    <div class="avatar" aria-hidden="true"><%# Ui.Initials((string)Eval("FullName")) %></div>
                                    <div class="item__main"><div class="item__title"><%#: Eval("FullName") %></div><div class="item__meta"><%#: Eval("Qualification") %> · <%#: Eval("Campus") %></div></div>
                                    <div class="item__side muted small"><%# Fmt.Ago((DateTime)Eval("SubmittedAt")) %></div>
                                </a>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoTutors" runat="server" />
                </div>
            </div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Deliveries to receive</h2><a href="<%: ResolveUrl("~/Operations/Deliveries.aspx") %>">Receive</a></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="DeliveryList" runat="server">
                            <ItemTemplate>
                                <div class="item"><div class="item__main"><div class="item__title"><%#: Eval("Supplier") %></div><div class="item__meta"><%#: Eval("Reference") %> · <%# Eval("Units") %> units · <%# Fmt.Ago((DateTime)Eval("DispatchedAt")) %></div></div></div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoDeliveries" runat="server" />
                </div>
            </div>
        </div>
        <div class="col-lg-6">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Overdue books</h2><a href="<%: ResolveUrl("~/Operations/Returns.aspx?view=overdue") %>">All overdue</a></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Overdue" runat="server">
                            <ItemTemplate>
                                <a class="item" href="<%# ResolveUrl("~/Operations/Returns.aspx?rental=" + Eval("RentalId")) %>">
                                    <div class="item__main"><div class="item__title"><%#: Eval("BookTitle") %></div><div class="item__meta"><%#: Eval("StudentName") %> · <%#: Eval("StudentNumber") %> · due <%# Fmt.Date((DateTime)Eval("DueDate")) %></div></div>
                                    <div class="item__side"><span class="badge badge--danger"><%# Eval("DaysOverdue") %> days</span></div>
                                </a>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoOverdue" runat="server" />
                </div>
            </div>
            <div class="action-list">
                <a class="action-card" href="<%: ResolveUrl("~/Operations/Returns.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("undo") %></span><span><span class="action-card__title d-block">Process a return</span><span class="action-card__text">Scan a copy barcode</span></span></a>
                <a class="action-card" href="<%: ResolveUrl("~/Operations/Users.aspx?new=1&role=Student") %>"><span class="action-card__icon"><%= Icons.Svg("users") %></span><span><span class="action-card__title d-block">Capture a student</span><span class="action-card__text">Create an account at the counter</span></span></a>
                <a class="action-card" href="<%: ResolveUrl("~/Operations/PasswordResets.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("key") %></span><span><span class="action-card__title d-block">Password resets</span><span class="action-card__text">Verify and send reset links</span></span></a>
            </div>
        </div>
    </div>
</asp:Content>
