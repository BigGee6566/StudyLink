<%@ Page Title="Fines" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Fines.aspx.cs" Inherits="StudyLink.Web.Operations.Fines" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:Literal ID="TabStrip" runat="server" />
    <asp:PlaceHolder ID="WaivePanel" runat="server" Visible="false">
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title">Waive fine</h2></div>
            <div class="panel__body">
                <p><asp:Literal ID="WaiveSummary" runat="server" /></p>
                <div class="filter-bar">
                    <div><label class="form-label" for="<%= WaiveReason.ClientID %>">Reason</label><asp:TextBox ID="WaiveReason" runat="server" CssClass="form-control" MaxLength="300" /></div>
                    <asp:Button ID="WaiveButton" runat="server" CssClass="btn btn-outline-danger" Text="Waive fine" OnClick="Waive_Click" />
                    <a class="btn btn-link" href="<%: ResolveUrl("~/Operations/Fines.aspx") %>">Cancel</a>
                </div>
            </div>
        </div>
    </asp:PlaceHolder>
    <div class="panel">
        <div class="panel__body">
            <asp:Panel runat="server" CssClass="filter-bar" DefaultButton="SearchButton">
                <div><label class="form-label" for="<%= SearchText.ClientID %>">Search</label><asp:TextBox ID="SearchText" runat="server" CssClass="form-control" placeholder="Student number, name or rental reference" /></div>
                <asp:Button ID="SearchButton" runat="server" CssClass="btn btn-primary" Text="Search" OnClick="Search_Click" />
            </asp:Panel>
        </div>
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Student</th><th>Book</th><th>Type</th><th>Raised</th><th class="col-num">Amount</th><th>Status</th><th class="col-action"></th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Student"><%#: Eval("StudentName") %><div class="muted small"><%#: Eval("StudentNumber") %></div></td>
                                <td data-label="Book"><%#: Eval("BookTitle") %><div class="muted small"><%#: Eval("RentalReference") %></div></td>
                                <td data-label="Type"><%#: Fmt.Words(Eval("Type")) %><%# (int)Eval("DaysLate") > 0 ? "<div class=\"muted small\">" + Eval("DaysLate") + " days late</div>" : "" %></td>
                                <td data-label="Raised"><%# Fmt.Date((DateTime)Eval("CreatedAt")) %></td>
                                <td data-label="Amount" class="col-num"><%# Fmt.Money(Eval("Amount")) %></td>
                                <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %><div class="muted small"><%#: Eval("WaiverReason") %></div></td>
                                <td class="col-action"><a class="btn btn-sm btn-outline-danger" href="<%# ResolveUrl("~/Operations/Fines.aspx?waive=" + Eval("FineId")) %>" <%# Eval("Status").ToString() == "Outstanding" ? "" : "hidden" %>>Waive</a></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="EmptyState" runat="server" />
        <div class="panel__footer d-flex justify-content-between"><span class="muted">Total shown</span><strong class="numeric"><asp:Literal ID="Total" runat="server" /></strong></div>
    </div>
</asp:Content>
