<%@ Page Title="Welcome back" Language="C#" MasterPageFile="~/MasterPages/Auth.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="StudyLink.Web.Account.Login" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC5 - Welcome Back (Login) --%>
    <h1>Welcome back</h1>
    <p class="lead">Log in to StudyLink with your username or email address.</p>
    <asp:Panel runat="server" DefaultButton="LoginButton">
        <div class="mb-3">
            <label class="form-label" for="<%= Identifier.ClientID %>">Username or email</label>
            <asp:TextBox ID="Identifier" runat="server" CssClass="form-control" MaxLength="100" autocomplete="username" />
        </div>
        <div class="mb-3">
            <label class="form-label" for="<%= Password.ClientID %>">Password</label>
            <asp:TextBox ID="Password" runat="server" CssClass="form-control" TextMode="Password" data-password-toggle="true" MaxLength="128" autocomplete="current-password" />
        </div>
        <div class="d-flex justify-content-between align-items-center mb-4">
            <div class="form-check">
                <asp:CheckBox ID="Remember" runat="server" CssClass="form-check-input-wrap" />
                <label class="form-check-label" for="<%= Remember.ClientID %>">Keep me signed in on this device</label>
            </div>
            <a href="<%: ResolveUrl("~/Account/ForgotPassword.aspx") %>">Forgot password?</a>
        </div>
        <asp:Button ID="LoginButton" runat="server" Text="Log in" CssClass="btn btn-primary btn-lg btn-block" OnClick="Login_Click" />
    </asp:Panel>
    <p class="text-center mt-4 mb-1 muted">New to StudyLink?</p>
    <div class="d-flex gap-2">
        <a class="btn btn-outline-primary flex-fill" href="<%: ResolveUrl("~/Account/Register.aspx?role=student") %>">Register as Student</a>
        <a class="btn btn-outline-primary flex-fill" href="<%: ResolveUrl("~/Account/Register.aspx?role=tutor") %>">Register as Tutor</a>
    </div>
    <p class="text-center mt-4 small"><a href="<%: ResolveUrl("~/Default.aspx") %>">&larr; Back to the StudyLink home page</a></p>
</asp:Content>