<%@ Page Title="Home" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="StudyLink.Web.Admin.Dashboard" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC21 - System Admin Dashboard --%>
    <asp:Literal ID="Kpis" runat="server" />
    <div class="row g-4">
        <div class="col-lg-7">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Recent activity</h2><a href="<%: ResolveUrl("~/Admin/AuditLog.aspx") %>">Audit log</a></div>
                <div class="table-wrap">
                    <table class="data-table stack">
                        <thead><tr><th>When</th><th>Who</th><th>Action</th><th>Record</th></tr></thead>
                        <tbody>
                            <asp:Repeater ID="Audit" runat="server">
                                <ItemTemplate>
                                    <tr>
                                        <td data-label="When"><%# Fmt.Ago((DateTime)Eval("CreatedAt")) %></td>
                                        <td data-label="Who"><%#: Eval("ActorName") %><div class="muted small"><%#: Fmt.Words(Eval("ActorRole")) %></div></td>
                                        <td data-label="Action"><%#: Fmt.Words(Eval("Action")) %></td>
                                        <td data-label="Record" class="small"><%#: Eval("EntityType") %> <%#: Eval("EntityId") %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
        <div class="col-lg-5">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Roles</h2><a href="<%: ResolveUrl("~/Admin/Roles.aspx") %>">Details</a></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Roles" runat="server">
                            <ItemTemplate>
                                <a class="item" href="<%# ResolveUrl("~/Operations/Users.aspx?role=" + Eval("Name")) %>">
                                    <div class="item__main"><div class="item__title"><%#: Eval("DisplayName") %></div><div class="item__meta"><%# (bool)Eval("SelfRegister") ? "Self-registers" : "Created by Admin / Manager" %></div></div>
                                    <div class="item__side"><span class="fw-semibold"><%# Eval("ActiveMembers") %></span><span class="muted small">of <%# Eval("TotalMembers") %> active</span></div>
                                </a>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">System</h2></div>
                <div class="panel__body">
                    <dl class="fact-list mb-3">
                        <dt>Database</dt><dd><asp:Literal ID="DbName" runat="server" /></dd>
                        <dt>Size</dt><dd><asp:Literal ID="DbSize" runat="server" /></dd>
                        <dt>Last backup</dt><dd><asp:Literal ID="LastBackup" runat="server" /></dd>
                        <dt>Locked accounts</dt><dd><asp:Literal ID="Locked" runat="server" /></dd>
                    </dl>
                    <div class="d-flex gap-2 flex-wrap">
                        <a class="btn btn-outline-primary btn-sm" href="<%: ResolveUrl("~/Admin/Backups.aspx") %>">Backups</a>
                        <a class="btn btn-outline-primary btn-sm" href="<%: ResolveUrl("~/Admin/Security.aspx") %>">Security</a>
                        <a class="btn btn-outline-primary btn-sm" href="<%: ResolveUrl("~/Admin/Settings.aspx") %>">Settings</a>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
