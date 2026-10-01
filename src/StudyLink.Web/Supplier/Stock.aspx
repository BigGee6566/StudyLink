<%@ Page Title="Stock" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Stock.aspx.cs" Inherits="StudyLink.Web.Supplier.Stock" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="panel">
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Title</th><th>ISBN</th><th class="col-num">Available at the hub</th><th class="col-num">Reorder level</th><th class="col-num">On order</th><th>Status</th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Title"><%#: Eval("Title") %></td>
                                <td data-label="ISBN" class="numeric"><%#: Eval("Isbn") %></td>
                                <td data-label="Available" class="col-num"><%# Eval("Available") %></td>
                                <td data-label="Reorder level" class="col-num"><%# Eval("ReorderLevel") %></td>
                                <td data-label="On order" class="col-num"><%# Eval("OnOrder") %></td>
                                <td data-label="Status"><%# (bool)Eval("IsLow") ? "<span class=\"badge badge--pending\">Low</span>" : "<span class=\"badge badge--success\">OK</span>" %></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="EmptyState" runat="server" />
    </div>
</asp:Content>
