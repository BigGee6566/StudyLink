<%@ Page Title="Forgot your password?" Language="C#" MasterPageFile="~/MasterPages/Auth.Master" AutoEventWireup="true" CodeBehind="ForgotPassword.aspx.cs" Inherits="StudyLink.Web.Account.ForgotPassword" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h1>Forgot your password?</h1>
    <p class="lead">Ask the hub to reset it. A staff member checks it's really you, then we email you a one-time link to choose a new password.</p>
    <asp:Panel ID="FormPanel" runat="server" DefaultButton="SendButton">
        <div class="mb-3"><label class="form-label" for="<%= Identifier.ClientID %>">Username, email or student number</label><asp:TextBox ID="Identifier" runat="server" CssClass="form-control" MaxLength="100" /></div>
        <div class="mb-3"><label class="form-label" for="<%= Message.ClientID %>">Anything staff should know? (optional)</label><asp:TextBox ID="Message" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" data-max="300" /></div>
        <asp:Button ID="SendButton" runat="server" Text="Request a password reset" CssClass="btn btn-primary btn-lg btn-block" OnClick="Send_Click" />
    </asp:Panel>
    <p class="text-center mt-4"><a href="<%: ResolveUrl("~/Account/Login.aspx") %>">&larr; Back to log in</a></p>
</asp:Content>