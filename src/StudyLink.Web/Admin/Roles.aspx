<%@ Page Title="Roles" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Roles.aspx.cs" Inherits="StudyLink.Web.Admin.Roles" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="row g-4">
        <asp:Repeater ID="List" runat="server">
            <ItemTemplate>
                <div class="col-lg-6">
                    <div class="panel h-100 mb-0">
                        <div class="panel__header"><h2 class="panel__title"><%#: Eval("DisplayName") %></h2>
                            <a class="btn btn-sm btn-outline-primary" href="<%# ResolveUrl("~/Operations/Users.aspx?role=" + Eval("Name")) %>"><%# Eval("ActiveMembers") %> active / <%# Eval("TotalMembers") %></a></div>
                        <div class="panel__body">
                            <p class="muted"><%#: Eval("Description") %></p>
                            <p class="small mb-2"><strong>How accounts are created:</strong> <%# (bool)Eval("SelfRegister") ? "self-registration" : "by a System Admin or Manager" %></p>
                            <div class="chips"><%# Chips(Eval("Permissions")) %></div>
                        </div>
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>
    <p class="muted small mt-4">Permissions come from one role–permission matrix used by menus, page guards and every service, so a role can never do more than its card shows. Assign or remove roles from a user's page in <a href="<%: ResolveUrl("~/Operations/Users.aspx") %>">Users</a>.</p>
</asp:Content>
