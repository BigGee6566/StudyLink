<%@ Page Title="Reviews" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Reviews.aspx.cs" Inherits="StudyLink.Web.Operations.Reviews" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:Literal ID="TabStrip" runat="server" />
    <div class="panel">
        <div class="panel__body">
            <div class="item-list">
                <asp:Repeater ID="Rows" runat="server" OnItemCommand="Row_Command">
                    <ItemTemplate>
                        <div class="item">
                            <div class="item__main">
                                <div><%# Ui.Stars((byte)Eval("Rating")) %> <%# (bool)Eval("IsHidden") ? "<span class=\"badge badge--danger\">Hidden</span>" : "" %></div>
                                <div class="mt-1"><%#: Eval("Comment") %></div>
                                <div class="item__meta"><%#: Eval("StudentName") %> → <%#: Eval("TutorName") %> · <%#: Eval("Subject") %> · <%# Fmt.Date((DateTime)Eval("CreatedAt")) %><%#: Eval("HiddenReason") == null ? "" : " · " + Eval("HiddenReason") %></div>
                            </div>
                            <div class="item__side">
                                <asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-danger" CommandName="hide" CommandArgument='<%# Eval("ReviewId") %>' Visible='<%# !(bool)Eval("IsHidden") %>' data-confirm="Hide this review from the tutor's profile and averages?">Hide</asp:LinkButton>
                                <asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-primary" CommandName="show" CommandArgument='<%# Eval("ReviewId") %>' Visible='<%# (bool)Eval("IsHidden") %>'>Restore</asp:LinkButton>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
            <asp:Literal ID="EmptyState" runat="server" />
            <div class="filter-bar mt-3">
                <div><asp:TextBox ID="Reason" runat="server" CssClass="form-control" MaxLength="200" placeholder="Reason for hiding (recorded in the audit log)" aria-label="Reason" /></div>
            </div>
        </div>
    </div>
</asp:Content>
