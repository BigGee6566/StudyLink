<%@ Page Title="First-run setup" Language="C#" MasterPageFile="~/MasterPages/Auth.Master" AutoEventWireup="true" CodeBehind="Setup.aspx.cs" Inherits="StudyLink.Web.Account.Setup" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h1>Set up StudyLink</h1>
    <asp:Panel ID="FormPanel" runat="server" DefaultButton="CreateButton">
        <p class="lead">No System Admin exists yet. Create the first administrator account; this page closes as soon as one exists.</p>
        <div class="row g-3">
            <div class="col-sm-6"><label class="form-label" for="<%= FirstName.ClientID %>">First name</label><asp:TextBox ID="FirstName" runat="server" CssClass="form-control" MaxLength="50" /></div>
            <div class="col-sm-6"><label class="form-label" for="<%= LastName.ClientID %>">Last name</label><asp:TextBox ID="LastName" runat="server" CssClass="form-control" MaxLength="50" /></div>
            <div class="col-12"><label class="form-label" for="<%= Email.ClientID %>">Email</label><asp:TextBox ID="Email" runat="server" CssClass="form-control" TextMode="Email" MaxLength="100" /></div>
            <div class="col-sm-6"><label class="form-label" for="<%= Cell.ClientID %>">Cell number</label><asp:TextBox ID="Cell" runat="server" CssClass="form-control" MaxLength="15" /></div>
            <div class="col-sm-6"><label class="form-label" for="<%= Campus.ClientID %>">Campus</label><asp:DropDownList ID="Campus" runat="server" CssClass="form-select" DataTextField="Name" DataValueField="Id" /></div>
            <div class="col-12"><label class="form-label" for="<%= UserName.ClientID %>">Username</label><asp:TextBox ID="UserName" runat="server" CssClass="form-control" MaxLength="50" /></div>
            <div class="col-sm-6"><label class="form-label" for="<%= Password.ClientID %>">Password</label><asp:TextBox ID="Password" runat="server" CssClass="form-control" TextMode="Password" /></div>
            <div class="col-sm-6"><label class="form-label" for="<%= Confirm.ClientID %>">Confirm password</label><asp:TextBox ID="Confirm" runat="server" CssClass="form-control" TextMode="Password" /></div>
        </div>
        <asp:Button ID="CreateButton" runat="server" Text="Create System Admin" CssClass="btn btn-primary btn-lg btn-block mt-4" OnClick="Create_Click" />
    </asp:Panel>
    <asp:Panel ID="ClosedPanel" runat="server" Visible="false">
        <p class="lead">Setup has already been completed.</p>
        <a class="btn btn-primary" href="<%: ResolveUrl("~/Account/Login.aspx") %>">Go to log in</a>
    </asp:Panel>
</asp:Content>