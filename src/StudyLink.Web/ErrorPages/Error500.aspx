<%@ Page Title="Something went wrong" Language="C#" MasterPageFile="~/MasterPages/Public.Master" AutoEventWireup="true" CodeBehind="Error500.aspx.cs" Inherits="StudyLink.Web.ErrorPages.Error500" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="public-main">
        <div class="confirm-card">
            <div class="confirm-card__icon" style="background:var(--red-100);color:var(--red-600)"><%= Icons.Svg("alert", 36) %></div>
            <h2>Something went wrong</h2>
            <p class="muted">The problem has been logged. If it keeps happening, contact the hub and quote this reference:</p>
            <p class="fs-4 fw-bold"><asp:Literal ID="Reference" runat="server" /></p>
            <a class="btn btn-primary" href="<%: ResolveUrl("~/Default.aspx") %>">StudyLink home</a>
        </div>
    </div>
</asp:Content>