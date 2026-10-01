<%@ Page Title="Password Reset Requests" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="PasswordResets.aspx.cs" Inherits="StudyLink.Web.Operations.PasswordResets" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC27 - Password Reset Requests --%>
    <asp:Literal ID="TabStrip" runat="server" />
    <asp:PlaceHolder ID="HandlePanel" runat="server" Visible="false">
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title">Verify and respond</h2></div>
            <div class="panel__body">
                <dl class="fact-list mb-3">
                    <dt>Entered</dt><dd><asp:Literal ID="Identifier" runat="server" /></dd>
                    <dt>Matched account</dt><dd><asp:Literal ID="Matched" runat="server" /></dd>
                    <dt>Message</dt><dd><asp:Literal ID="Message" runat="server" /></dd>
                    <dt>Requested</dt><dd><asp:Literal ID="Requested" runat="server" /></dd>
                </dl>
                <div class="alert-box alert-box--info"><div>Confirm the person's identity (e.g. student card at the counter or a call to their registered cell) before approving. Approving emails them a single-use reset link; no password is ever shown.</div></div>
                <label class="form-label" for="<%= Note.ClientID %>">Note</label>
                <asp:TextBox ID="Note" runat="server" CssClass="form-control mb-3" MaxLength="300" placeholder="How the identity was verified" />
                <div class="d-flex gap-2">
                    <asp:Button ID="ApproveButton" runat="server" CssClass="btn btn-primary" Text="Approve and email link" OnClick="Approve_Click" />
                    <asp:Button ID="RejectButton" runat="server" CssClass="btn btn-outline-danger" Text="Reject" OnClick="Reject_Click" data-confirm="Reject this request?" />
                </div>
            </div>
        </div>
    </asp:PlaceHolder>
    <div class="panel">
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Requested</th><th>Entered</th><th>Matched account</th><th>Message</th><th>Status</th><th class="col-action"></th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Requested"><%# Fmt.DateTime((DateTime)Eval("CreatedAt")) %><div class="muted small"><%#: Eval("RequestIp") %></div></td>
                                <td data-label="Entered"><%#: Eval("Identifier") %></td>
                                <td data-label="Matched"><%# Eval("UserId") == null ? "<span class=\"badge badge--danger\">No match</span>" : Fmt.H(Eval("MatchedUser")) + "<div class=\"muted small\">" + Fmt.H(Eval("MatchedEmail")) + " · " + Fmt.H(Eval("Roles")) + "</div>" %></td>
                                <td data-label="Message" class="small"><%#: Eval("Message") %></td>
                                <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %><div class="muted small"><%#: Eval("HandledBy") %></div></td>
                                <td class="col-action"><a class="btn btn-sm btn-outline-primary" href="<%# ResolveUrl("~/Operations/PasswordResets.aspx?id=" + Eval("RequestId")) %>" <%# Eval("Status").ToString() == "Pending" ? "" : "hidden" %>>Handle</a></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="EmptyState" runat="server" />
    </div>
</asp:Content>
