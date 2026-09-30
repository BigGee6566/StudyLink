<%@ Page Title="Change password" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="ChangePassword.aspx.cs" Inherits="StudyLink.Web.Account.ChangePassword" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="panel" style="max-width:560px">
        <div class="panel__body">
            <asp:Panel runat="server" DefaultButton="SaveButton">
                <div class="mb-3"><label class="form-label" for="<%= Current.ClientID %>">Current password</label><asp:TextBox ID="Current" runat="server" CssClass="form-control" TextMode="Password" autocomplete="current-password" /></div>
                <div class="mb-3"><label class="form-label" for="<%= NewPassword.ClientID %>">New password</label><asp:TextBox ID="NewPassword" runat="server" CssClass="form-control" TextMode="Password" autocomplete="new-password" /><div class="form-hint">At least 8 characters, with a letter and a number.</div></div>
                <div class="mb-3"><label class="form-label" for="<%= Confirm.ClientID %>">Confirm new password</label><asp:TextBox ID="Confirm" runat="server" CssClass="form-control" TextMode="Password" autocomplete="new-password" /></div>
                <asp:Button ID="SaveButton" runat="server" Text="Change password" CssClass="btn btn-primary" OnClick="Save_Click" />
            </asp:Panel>
        </div>
    </div>
</asp:Content>