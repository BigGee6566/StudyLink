<%@ Page Title="Audit Logs" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="AuditLog.aspx.cs" Inherits="StudyLink.Web.Admin.AuditLog" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC28 - Audit Logs (append-only) --%>
    <div class="panel">
        <div class="panel__body">
            <asp:Panel runat="server" CssClass="filter-bar" DefaultButton="SearchButton">
                <div><label class="form-label" for="<%= ActionFilter.ClientID %>">Action</label><asp:DropDownList ID="ActionFilter" runat="server" CssClass="form-select" /></div>
                <div><label class="form-label" for="<%= EntityFilter.ClientID %>">Record type</label><asp:TextBox ID="EntityFilter" runat="server" CssClass="form-control" placeholder="e.g. Booking, Payment, User" /></div>
                <div><label class="form-label" for="<%= ActorFilter.ClientID %>">User</label><asp:TextBox ID="ActorFilter" runat="server" CssClass="form-control" placeholder="Username" /></div>
                <div><label class="form-label" for="<%= From.ClientID %>">From</label><asp:TextBox ID="From" runat="server" CssClass="form-control" TextMode="Date" /></div>
                <div><label class="form-label" for="<%= To.ClientID %>">To</label><asp:TextBox ID="To" runat="server" CssClass="form-control" TextMode="Date" /></div>
                <asp:Button ID="SearchButton" runat="server" CssClass="btn btn-primary" Text="Filter" OnClick="Search_Click" />
            </asp:Panel>
        </div>
    </div>
    <div class="panel">
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>When</th><th>User</th><th>Action</th><th>Record</th><th>Change</th><th>IP</th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="When" class="text-nowrap"><%# Fmt.DateTime((DateTime)Eval("CreatedAt")) %></td>
                                <td data-label="User"><%#: Eval("ActorName") %><div class="muted small"><%#: Fmt.Words(Eval("ActorRole")) %></div></td>
                                <td data-label="Action"><%#: Fmt.Words(Eval("Action")) %></td>
                                <td data-label="Record"><%#: Eval("EntityType") %><div class="muted small numeric"><%#: Eval("EntityId") %></div></td>
                                <td data-label="Change" class="small"><%# Change(Container.DataItem) %></td>
                                <td data-label="IP" class="small"><%#: Eval("IpAddress") %></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="EmptyState" runat="server" />
        <asp:Literal ID="Pager" runat="server" />
    </div>
</asp:Content>
