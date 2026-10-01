<%@ Page Title="Contact & Support" Language="C#" AutoEventWireup="true" CodeBehind="Contact.aspx.cs" Inherits="StudyLink.Web.Public.Contact" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="detail-grid">
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title">Send us a message</h2></div>
            <div class="panel__body">
                <asp:Panel ID="FormPanel" runat="server" DefaultButton="SendButton">
                    <div class="row g-3">
                        <div class="col-md-6"><label class="form-label" for="<%= SenderName.ClientID %>">Your name</label><asp:TextBox ID="SenderName" runat="server" CssClass="form-control" MaxLength="100" /></div>
                        <div class="col-md-6"><label class="form-label" for="<%= SenderEmail.ClientID %>">Email</label><asp:TextBox ID="SenderEmail" runat="server" CssClass="form-control" TextMode="Email" MaxLength="150" /></div>
                        <div class="col-md-6"><label class="form-label" for="<%= Campus.ClientID %>">Campus</label><asp:DropDownList ID="Campus" runat="server" CssClass="form-select" /></div>
                        <div class="col-md-6"><label class="form-label" for="<%= Subject.ClientID %>">Subject</label><asp:TextBox ID="Subject" runat="server" CssClass="form-control" MaxLength="150" /></div>
                        <div class="col-12"><label class="form-label" for="<%= Body.ClientID %>">Message</label><asp:TextBox ID="Body" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="6" MaxLength="2000" data-max="2000" /></div>
                    </div>
                    <div class="form-actions"><asp:Button ID="SendButton" runat="server" CssClass="btn btn-primary" Text="Send message" OnClick="Send_Click" data-once="Sending…" /></div>
                </asp:Panel>
                <asp:Literal ID="Sent" runat="server" />
            </div>
        </div>
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title">Forte Books &amp; Tuition Hub</h2></div>
            <div class="panel__body">
                <p>Visit the hub counter at any of our three campuses for book collection, returns and help with your account.</p>
                <ul class="list-unstyled mb-3">
                    <li class="mb-2"><%= Icons.Svg("pin", 16) %> Alice campus</li>
                    <li class="mb-2"><%= Icons.Svg("pin", 16) %> East London campus</li>
                    <li class="mb-2"><%= Icons.Svg("pin", 16) %> Bisho campus</li>
                </ul>
                <p class="muted small mb-0">Forgot your password? Use <a href="<%: ResolveUrl("~/Account/ForgotPassword.aspx") %>">Forgot password</a> and hub staff will verify you and email a reset link.</p>
            </div>
        </div>
    </div>
</asp:Content>