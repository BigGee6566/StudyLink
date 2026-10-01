<%@ Page Title="Access denied" Language="C#" MasterPageFile="~/MasterPages/Public.Master" AutoEventWireup="true" CodeBehind="AccessDenied.aspx.cs" Inherits="StudyLink.Web.Account.AccessDenied" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="public-main">
        <div class="confirm-card">
            <div class="confirm-card__icon" style="background:var(--red-100);color:var(--red-600)"><%= Icons.Svg("lock", 36) %></div>
            <h2>You don't have access to that page</h2>
            <p class="muted">Your role does not include this part of StudyLink. If you think it should, contact the hub.</p>
            <a class="btn btn-primary" href="<%: ResolveUrl(HomeUrl) %>">Go to my dashboard</a>
        </div>
    </div>
</asp:Content>