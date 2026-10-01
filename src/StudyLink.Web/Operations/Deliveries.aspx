<%@ Page Title="Deliveries" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Deliveries.aspx.cs" Inherits="StudyLink.Web.Operations.Deliveries" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:Repeater ID="List" runat="server" OnItemCommand="Delivery_Command">
        <ItemTemplate>
            <div class="panel">
                <div class="panel__header">
                    <h2 class="panel__title"><%#: Eval("Reference") %> · <%#: Eval("Supplier") %></h2>
                    <span class="muted small">Order <%#: Eval("OrderReference") %> · dispatched <%# Fmt.DateTime((DateTime)Eval("DispatchedAt")) %></span>
                </div>
                <div class="table-wrap">
                    <table class="data-table stack">
                        <thead><tr><th>Title</th><th>Campus</th><th class="col-num">Dispatched</th><th class="col-num">Accepted</th></tr></thead>
                        <tbody>
                            <asp:Repeater ID="Lines" runat="server" DataSource='<%# Eval("Lines") %>'>
                                <ItemTemplate>
                                    <tr>
                                        <td data-label="Title"><%#: Eval("Title") %></td>
                                        <td data-label="Campus"><%#: Eval("Campus") %></td>
                                        <td data-label="Dispatched" class="col-num"><%# Eval("Quantity") %></td>
                                        <td data-label="Accepted" class="col-num">
                                            <asp:HiddenField ID="LineId" runat="server" Value='<%# Eval("DeliveryLineId") %>' />
                                            <asp:TextBox ID="Accepted" runat="server" CssClass="form-control form-control-sm text-end ms-auto" style="max-width:90px" Text='<%# Eval("Quantity") %>' TextMode="Number" />
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
                <div class="panel__footer">
                    <div class="filter-bar">
                        <div><asp:TextBox ID="Note" runat="server" CssClass="form-control" MaxLength="300" placeholder="Note (e.g. 2 copies damaged in transit)" aria-label="Note" /></div>
                        <asp:Button runat="server" CssClass="btn btn-primary" Text="Receive into stock" CommandName="receive" CommandArgument='<%# Eval("DeliveryId") %>' data-once="Receiving…" />
                        <asp:Button runat="server" CssClass="btn btn-outline-danger" Text="Reject delivery" CommandName="reject" CommandArgument='<%# Eval("DeliveryId") %>' data-confirm="Reject the whole delivery?" />
                    </div>
                </div>
            </div>
        </ItemTemplate>
    </asp:Repeater>
    <asp:Literal ID="EmptyState" runat="server" />
</asp:Content>
