<%@ Page Title="Book Management" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Books.aspx.cs" Inherits="StudyLink.Web.Manager.Books" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC25 - Book Management (catalogue) and Inventory (stock derived from copy statuses) --%>
    <asp:Literal ID="TabStrip" runat="server" />
    <div class="panel">
        <div class="panel__body">
            <asp:Panel runat="server" CssClass="filter-bar" DefaultButton="SearchButton">
                <div><label class="form-label" for="<%= SearchText.ClientID %>">Search</label><asp:TextBox ID="SearchText" runat="server" CssClass="form-control" placeholder="Title, author or ISBN" /></div>
                <div><label class="form-label" for="<%= Category.ClientID %>">Category</label><asp:DropDownList ID="Category" runat="server" CssClass="form-select" /></div>
                <div><label class="form-label" for="<%= Campus.ClientID %>">Campus</label><asp:DropDownList ID="Campus" runat="server" CssClass="form-select" /></div>
                <div class="filter-actions d-flex align-items-center gap-3">
                    <div class="form-check mb-0"><asp:CheckBox ID="LowOnly" runat="server" /><label class="form-check-label" for="<%= LowOnly.ClientID %>">Low stock</label></div>
                    <div class="form-check mb-0"><asp:CheckBox ID="Inactive" runat="server" /><label class="form-check-label" for="<%= Inactive.ClientID %>">Include retired</label></div>
                    <asp:Button ID="SearchButton" runat="server" CssClass="btn btn-primary" Text="Search" OnClick="Search_Click" />
                    <a class="btn btn-accent" href="<%: ResolveUrl("~/Manager/BookEdit.aspx") %>">+ Add book</a>
                </div>
            </asp:Panel>
        </div>
    </div>
    <div class="panel">
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><asp:Literal ID="Head" runat="server" /></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Title"><a href="<%# ResolveUrl("~/Manager/BookEdit.aspx?id=" + Eval("BookId")) %>"><%#: Eval("Title") %></a><div class="muted small"><%#: Eval("Isbn") %> · <%#: Eval("Category") %></div></td>
                                <%# InventoryView ? StockCells(Container.DataItem) : PriceCells(Container.DataItem) %>
                                <td data-label="Status"><%# StatusOf(Container.DataItem) %></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="EmptyState" runat="server" />
        <asp:Literal ID="Pager" runat="server" />
    </div>
</asp:Content>
