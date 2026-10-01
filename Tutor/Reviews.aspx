<%@ Page Title="My Reviews" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Reviews.aspx.cs" Inherits="StudyLink.Web.Tutor.Reviews" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:Literal ID="Summary" runat="server" />
    <div class="panel">
        <div class="panel__body">
            <div class="item-list">
                <asp:Repeater ID="List" runat="server">
                    <ItemTemplate>
                        <div class="item">
                            <div class="item__main">
                                <div><%# Ui.Stars((byte)Eval("Rating")) %></div>
                                <div class="mt-1"><%#: Eval("Comment") %></div>
                                <div class="item__meta"><%#: Eval("Subject") %> · <%# Fmt.Date((DateTime)Eval("CreatedAt")) %></div>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
            <asp:Literal ID="EmptyState" runat="server" />
        </div>
    </div>
</asp:Content>