<%@ Page Title="Rentals & Returns" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Returns.aspx.cs" Inherits="StudyLink.Web.Operations.Returns" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="panel">
        <div class="panel__body">
            <asp:Panel runat="server" CssClass="filter-bar" DefaultButton="FindButton">
                <div><label class="form-label" for="<%= Barcode.ClientID %>">Scan or type a copy barcode or rental reference</label>
                    <asp:TextBox ID="Barcode" runat="server" CssClass="form-control" MaxLength="40" autocomplete="off" /></div>
                <asp:Button ID="FindButton" runat="server" CssClass="btn btn-primary" Text="Find rental" OnClick="Find_Click" />
            </asp:Panel>
        </div>
    </div>

    <asp:PlaceHolder ID="ReturnPanel" runat="server" Visible="false">
        <div class="detail-grid">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Rental <asp:Literal ID="RentalRef" runat="server" /></h2><asp:Literal ID="RentalStatus" runat="server" /></div>
                <div class="panel__body">
                    <dl class="fact-list">
                        <dt>Book</dt><dd><asp:Literal ID="BookTitle" runat="server" /></dd>
                        <dt>Copy</dt><dd><asp:Literal ID="CopyBarcode" runat="server" /></dd>
                        <dt>Student</dt><dd><asp:Literal ID="StudentName" runat="server" /></dd>
                        <dt>Campus</dt><dd><asp:Literal ID="CampusName" runat="server" /></dd>
                        <dt>Rented</dt><dd><asp:Literal ID="Rented" runat="server" /></dd>
                        <dt>Due</dt><dd><asp:Literal ID="Due" runat="server" /></dd>
                        <dt>Overdue fine if returned now</dt><dd><asp:Literal ID="Accrued" runat="server" /></dd>
                        <dt>Outstanding fines</dt><dd><asp:Literal ID="Outstanding" runat="server" /></dd>
                    </dl>
                </div>
            </div>
            <asp:PlaceHolder ID="ReturnForm" runat="server">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title">Process return</h2></div>
                    <div class="panel__body">
                        <div class="mb-2"><label class="form-label" for="<%= Condition.ClientID %>">Condition on return</label><asp:DropDownList ID="Condition" runat="server" CssClass="form-select" /></div>
                        <div class="mb-2"><label class="form-label" for="<%= DamageFine.ClientID %>">Damage fine (R)</label><asp:TextBox ID="DamageFine" runat="server" CssClass="form-control" Text="0" inputmode="decimal" /></div>
                        <div class="mb-3"><label class="form-label" for="<%= Notes.ClientID %>">Notes</label><asp:TextBox ID="Notes" runat="server" CssClass="form-control" MaxLength="300" /></div>
                        <div class="d-flex gap-2 flex-wrap">
                            <asp:Button ID="ReturnButton" runat="server" CssClass="btn btn-primary" Text="Receive book" OnClick="Return_Click" data-once="Processing…" />
                            <asp:Button ID="LostButton" runat="server" CssClass="btn btn-outline-danger" Text="Mark as lost" OnClick="Lost_Click" data-confirm="Mark this copy as lost? The student is charged the replacement price." />
                        </div>
                        <p class="form-hint mt-2">Late returns are fined automatically at the title's daily late fee, capped at its sale price.</p>
                    </div>
                </div>
            </asp:PlaceHolder>
        </div>
    </asp:PlaceHolder>

    <asp:Literal ID="TabStrip" runat="server" />
    <div class="panel">
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Book</th><th>Student</th><th>Campus</th><th>Due</th><th>Status</th><th class="col-action"></th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Book"><%#: Eval("BookTitle") %><div class="muted small"><%#: Eval("Reference") %> · <%#: Eval("Barcode") %></div></td>
                                <td data-label="Student"><%#: Eval("StudentName") %><div class="muted small"><%#: Eval("StudentNumber") %></div></td>
                                <td data-label="Campus"><%#: Eval("Campus") %></td>
                                <td data-label="Due"><%# Fmt.Date((DateTime)Eval("DueDate")) %></td>
                                <td data-label="Status"><%# (bool)Eval("IsOverdue") ? "<span class=\"badge badge--danger\">" + Eval("DaysOverdue") + " days overdue</span>" : Fmt.Badge(Eval("Status")) %></td>
                                <td class="col-action"><a class="btn btn-sm btn-outline-primary" href="<%# ResolveUrl("~/Operations/Returns.aspx?rental=" + Eval("RentalId")) %>">Open</a></td>
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
