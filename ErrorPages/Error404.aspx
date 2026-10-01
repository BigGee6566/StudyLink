<%@ Page Title="Page not found" Language="C#" MasterPageFile="~/MasterPages/Public.Master" AutoEventWireup="true" CodeBehind="Error404.aspx.cs" Inherits="StudyLink.Web.ErrorPages.Error404" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="public-main">
        <div class="confirm-card">
            <div class="confirm-card__icon" style="background:var(--blue-100);color:var(--blue-600)"><%= Icons.Svg("search", 36) %></div>
            <h2>We couldn't find that page</h2>
            <p class="muted">The link may be old or mistyped.</p>
            <a class="btn btn-primary" href="<%: ResolveUrl("~/Default.aspx") %>">StudyLink home</a>
        </div>
    </div>
</asp:Content>