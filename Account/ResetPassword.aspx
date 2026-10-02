<%@ Page Title="Choose a new password" Language="C#" MasterPageFile="~/MasterPages/Auth.Master" AutoEventWireup="true" CodeBehind="ResetPassword.aspx.cs" Inherits="StudyLink.Web.Account.ResetPassword" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h1>Choose a new password</h1>
    <asp:Panel ID="FormPanel" runat="server" DefaultButton="SaveButton">
        <p class="lead">This link works once. Choose a password with at least 8 characters, including a letter and a number.</p>
        <div class="mb-3"><label class="form-label" for="<%= NewPassword.ClientID %>">New password</label><asp:TextBox ID="NewPassword" runat="server" CssClass="form-control" TextMode="Password" data-password-toggle="true" autocomplete="new-password" /></div>
        <div class="mb-3"><label class="form-label" for="<%= Confirm.ClientID %>">Confirm new password</label><asp:TextBox ID="Confirm" runat="server" CssClass="form-control" TextMode="Password" data-password-toggle="true" autocomplete="new-password" /></div>
        <asp:Button ID="SaveButton" runat="server" Text="Save new password" CssClass="btn btn-primary btn-lg btn-block" OnClick="Save_Click" />
    </asp:Panel>
    <asp:Panel ID="InvalidPanel" runat="server" Visible="false">
        <p class="lead">This link is invalid or has expired. Links work once and only for a limited time.</p>
        <a class="btn btn-primary" href="<%: ResolveUrl("~/Account/ForgotPassword.aspx") %>">Request a new link</a>
    </asp:Panel>
</asp:Content>