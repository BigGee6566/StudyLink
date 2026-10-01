<%@ Page Title="Notifications" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="StudyLink.Web.Notifications.Index" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="panel">
        <div class="panel__header"><h2 class="panel__title">All notifications</h2>
            <asp:Button ID="MarkAll" runat="server" CssClass="btn btn-sm btn-outline-primary" Text="Mark all as read" OnClick="MarkAll_Click" /></div>
        <div class="panel__body">
            <div class="item-list">
                <asp:Repeater ID="List" runat="server">
                    <ItemTemplate>
                        <a class="item<%# (bool)Eval("IsRead") ? "" : " border-primary" %>" href="<%# ResolveUrl("~/Notifications/Index.aspx?open=" + Eval("NotificationId")) %>">
                            <span class="action-card__icon"><%# Icons.Svg("bell") %></span>
                            <div class="item__main"><div class="item__title"><%#: Eval("Title") %></div><div class="item__meta"><%#: Eval("Message") %></div></div>
                            <div class="item__side"><span class="muted small text-nowrap"><%# Fmt.Ago((DateTime)Eval("CreatedAt")) %></span><%# (bool)Eval("IsRead") ? "" : "<span class=\"badge badge--info\">New</span>" %></div>
                        </a>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
            <asp:Literal ID="EmptyState" runat="server" />
        </div>
        <asp:Literal ID="Pager" runat="server" />
    </div>
</asp:Content>