<%@ Page Title="Support" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Support.aspx.cs" Inherits="StudyLink.Web.Operations.Support" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:Literal ID="TabStrip" runat="server" />
    <asp:MultiView ID="Views" runat="server" ActiveViewIndex="0">
        <asp:View ID="MessagesView" runat="server">
            <asp:PlaceHolder ID="RespondPanel" runat="server" Visible="false">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title"><asp:Literal ID="MsgSubject" runat="server" /></h2><asp:Literal ID="MsgStatus" runat="server" /></div>
                    <div class="panel__body">
                        <p class="muted small mb-2"><asp:Literal ID="MsgFrom" runat="server" /></p>
                        <p style="white-space:pre-wrap"><asp:Literal ID="MsgBody" runat="server" /></p>
                        <div class="row g-2">
                            <div class="col-md-3"><asp:DropDownList ID="NewStatus" runat="server" CssClass="form-select" aria-label="Status" /></div>
                            <div class="col-md-9"><asp:TextBox ID="ResponseText" runat="server" CssClass="form-control" MaxLength="500" placeholder="Response (emailed to the sender when resolved)" /></div>
                        </div>
                        <div class="form-actions"><asp:Button ID="RespondButton" runat="server" CssClass="btn btn-primary" Text="Save" OnClick="Respond_Click" /></div>
                    </div>
                </div>
            </asp:PlaceHolder>
            <div class="panel">
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Messages" runat="server">
                            <ItemTemplate>
                                <a class="item" href="<%# ResolveUrl("~/Operations/Support.aspx?id=" + Eval("MessageId")) %>">
                                    <div class="item__main"><div class="item__title"><%#: Eval("Subject") %></div>
                                        <div class="item__meta"><%#: Eval("Name") %> · <%#: Eval("Email") %><%#: Eval("Campus") == null ? "" : " · " + Eval("Campus") %> · <%# Fmt.Ago((DateTime)Eval("CreatedAt")) %></div></div>
                                    <div class="item__side"><%# Fmt.Badge(Eval("Status")) %></div>
                                </a>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoMessages" runat="server" />
                </div>
            </div>
        </asp:View>
        <asp:View ID="ProfileView" runat="server">
            <div class="panel">
                <div class="table-wrap">
                    <table class="data-table stack">
                        <thead><tr><th>User</th><th>Change</th><th>Reason</th><th>Requested</th><th>Status</th><th class="col-action"></th></tr></thead>
                        <tbody>
                            <asp:Repeater ID="ProfileRequests" runat="server" OnItemCommand="Profile_Command">
                                <ItemTemplate>
                                    <tr>
                                        <td data-label="User"><%#: Eval("FullName") %><div class="muted small"><%#: Eval("UserName") %></div></td>
                                        <td data-label="Change"><%#: Fmt.Words(Eval("FieldName")) %>: <span class="muted"><%#: Eval("CurrentValue") %></span> → <strong><%#: Eval("RequestedValue") %></strong></td>
                                        <td data-label="Reason" class="small"><%#: Eval("Reason") %></td>
                                        <td data-label="Requested"><%# Fmt.Date((DateTime)Eval("CreatedAt")) %></td>
                                        <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %><div class="muted small"><%#: Eval("ResponseNote") %></div></td>
                                        <td class="col-action">
                                            <asp:LinkButton runat="server" CssClass="btn btn-sm btn-primary" CommandName="approve" CommandArgument='<%# Eval("RequestId") %>' Visible='<%# Eval("Status").ToString() == "Pending" %>'>Approve</asp:LinkButton>
                                            <asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-danger" CommandName="reject" CommandArgument='<%# Eval("RequestId") %>' Visible='<%# Eval("Status").ToString() == "Pending" %>' data-confirm="Reject this change request?">Reject</asp:LinkButton>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
                <asp:Literal ID="NoProfileRequests" runat="server" />
            </div>
        </asp:View>
    </asp:MultiView>
</asp:Content>
