<%@ Page Title="Security" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Security.aspx.cs" Inherits="StudyLink.Web.Admin.Security" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:Literal ID="Policy" runat="server" />
    <div class="row g-4">
        <div class="col-lg-6">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Locked accounts</h2></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="LockedList" runat="server" OnItemCommand="Locked_Command">
                            <ItemTemplate>
                                <div class="item">
                                    <div class="item__main"><div class="item__title"><%#: Eval("FullName") %></div><div class="item__meta"><%#: Eval("UserName") %> · <%#: Eval("Roles") %></div></div>
                                    <div class="item__side"><asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-primary" CommandName="unlock" CommandArgument='<%# Eval("UserId") %>'>Unlock</asp:LinkButton></div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoLocked" runat="server" />
                </div>
            </div>
        </div>
        <div class="col-lg-6">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Recent security events</h2><a href="<%: ResolveUrl("~/Admin/AuditLog.aspx") %>">Audit log</a></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="EventList" runat="server">
                            <ItemTemplate>
                                <div class="item"><div class="item__main"><div class="item__title"><%#: Fmt.Words(Eval("Action")) %></div>
                                    <div class="item__meta"><%#: Eval("ActorName") %> · <%#: Eval("EntityType") %> <%#: Eval("EntityId") %> · <%#: Eval("IpAddress") %></div></div>
                                    <div class="item__side muted small"><%# Fmt.Ago((DateTime)Eval("CreatedAt")) %></div></div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoEvents" runat="server" />
                </div>
            </div>
        </div>
    </div>
    <div class="panel">
        <div class="panel__header"><h2 class="panel__title">How StudyLink protects accounts</h2></div>
        <div class="panel__body small">
            <ul class="mb-0">
                <li>Passwords are stored only as salted PBKDF2-SHA256 hashes (100,000 iterations).</li>
                <li>Repeated failed sign-ins lock the account for a period; Admins can unlock early here.</li>
                <li>Roles and the active flag are re-read from the database on every request, so deactivation takes effect immediately.</li>
                <li>Every page postback is bound to the signed-in session (anti-forgery), and pages send strict security headers.</li>
                <li>Password resets are verified by staff and completed with a single-use, time-limited emailed link.</li>
                <li>Card numbers and CVVs are never stored; only the card brand and last four digits.</li>
            </ul>
        </div>
    </div>
</asp:Content>
