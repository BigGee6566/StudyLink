<%@ Page Title="Categories" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Categories.aspx.cs" Inherits="StudyLink.Web.Manager.Categories" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="detail-grid">
        <div class="panel">
            <div class="table-wrap">
                <table class="data-table stack">
                    <thead><tr><th>Category</th><th class="col-num">Titles</th><th>Status</th><th class="col-action"></th></tr></thead>
                    <tbody>
                        <asp:Repeater ID="Rows" runat="server" OnItemCommand="Row_Command">
                            <ItemTemplate>
                                <tr>
                                    <td data-label="Category"><%#: Eval("Name") %><div class="muted small"><%#: Eval("Description") %></div></td>
                                    <td data-label="Titles" class="col-num"><%# Eval("Books") %></td>
                                    <td data-label="Status"><%# Fmt.Badge((bool)Eval("IsActive") ? "Active" : "Inactive") %></td>
                                    <td class="col-action">
                                        <a class="btn btn-sm btn-outline-primary" href="<%# ResolveUrl("~/Manager/Categories.aspx?edit=" + Eval("CategoryId")) %>">Edit</a>
                                        <asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-secondary" CommandName="toggle" CommandArgument='<%# Eval("CategoryId") + ":" + Eval("IsActive") %>'><%# (bool)Eval("IsActive") ? "Retire" : "Restore" %></asp:LinkButton>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
        </div>
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title"><asp:Literal ID="FormTitle" runat="server" Text="Add a category" /></h2></div>
            <div class="panel__body">
                <div class="mb-2"><label class="form-label" for="<%= CategoryName.ClientID %>">Name</label><asp:TextBox ID="CategoryName" runat="server" CssClass="form-control" MaxLength="80" /></div>
                <div class="mb-3"><label class="form-label" for="<%= CategoryDescription.ClientID %>">Description</label><asp:TextBox ID="CategoryDescription" runat="server" CssClass="form-control" MaxLength="250" /></div>
                <asp:Button ID="SaveButton" runat="server" CssClass="btn btn-primary btn-block" Text="Save" OnClick="Save_Click" />
            </div>
        </div>
    </div>
</asp:Content>
