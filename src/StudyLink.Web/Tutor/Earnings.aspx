<%@ Page Title="Earnings" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Earnings.aspx.cs" Inherits="StudyLink.Web.Tutor.Earnings" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="panel">
        <div class="panel__body">
            <div class="filter-bar">
                <div><label class="form-label" for="<%= From.ClientID %>">From</label><asp:TextBox ID="From" runat="server" CssClass="form-control" TextMode="Date" /></div>
                <div><label class="form-label" for="<%= To.ClientID %>">To</label><asp:TextBox ID="To" runat="server" CssClass="form-control" TextMode="Date" /></div>
                <asp:Button ID="ShowButton" runat="server" CssClass="btn btn-primary" Text="Show" OnClick="Show_Click" />
            </div>
        </div>
    </div>
    <asp:Literal ID="Kpis" runat="server" />
    <div class="panel">
        <div class="panel__header"><h2 class="panel__title">Completed sessions</h2><span class="muted small"><asp:Literal ID="CommissionNote" runat="server" /></span></div>
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Date</th><th>Reference</th><th>Student</th><th>Subject</th><th class="col-num">Hours</th><th class="col-num">Fee</th><th class="col-num">Commission</th><th class="col-num">You earn</th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Lines" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Date"><%# Fmt.Date((DateTime)Eval("SessionDate")) %></td>
                                <td data-label="Reference" class="numeric"><%#: Eval("Reference") %></td>
                                <td data-label="Student"><%#: Eval("StudentName") %></td>
                                <td data-label="Subject"><%#: Eval("Subject") %></td>
                                <td data-label="Hours" class="col-num"><%# Fmt.Hours((decimal)Eval("Hours")) %></td>
                                <td data-label="Fee" class="col-num"><%# Fmt.Money(Eval("Fee")) %></td>
                                <td data-label="Commission" class="col-num"><%# Fmt.Money(Eval("Commission")) %></td>
                                <td data-label="You earn" class="col-num fw-semibold"><%# Fmt.Money(Eval("Net")) %></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="EmptyState" runat="server" />
    </div>
</asp:Content>