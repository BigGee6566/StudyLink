<%@ Page Title="Timesheets" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Timesheets.aspx.cs" Inherits="StudyLink.Web.Operations.Timesheets" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:Literal ID="TabStrip" runat="server" />
    <asp:PlaceHolder ID="DetailPanel" runat="server" Visible="false">
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title"><asp:Literal ID="SheetTitle" runat="server" /></h2><asp:Literal ID="SheetStatus" runat="server" /></div>
            <div class="table-wrap">
                <table class="data-table stack">
                    <thead><tr><th>Date</th><th>Work</th><th class="col-num">Hours</th></tr></thead>
                    <tbody>
                        <asp:Repeater ID="Entries" runat="server">
                            <ItemTemplate>
                                <tr>
                                    <td data-label="Date"><%# Fmt.Day((DateTime)Eval("WorkDate")) %></td>
                                    <td data-label="Work"><%#: Eval("Description") %><%# Eval("BookingReference") == null ? "<div class=\"muted small\">Logged by tutor</div>" : "<div class=\"muted small\">Attended session " + Fmt.H(Eval("BookingReference")) + "</div>" %></td>
                                    <td data-label="Hours" class="col-num"><%# Fmt.Hours((decimal)Eval("Hours")) %></td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
            <div class="panel__footer">
                <div class="d-flex justify-content-between mb-2"><span>Total</span><strong><asp:Literal ID="SheetTotal" runat="server" /></strong></div>
                <asp:PlaceHolder ID="ReviewForm" runat="server">
                    <div class="filter-bar">
                        <div><asp:TextBox ID="Note" runat="server" CssClass="form-control" MaxLength="300" placeholder="Note to the tutor (required when sending back)" aria-label="Note" /></div>
                        <asp:Button ID="ApproveButton" runat="server" CssClass="btn btn-primary" Text="Approve" OnClick="Approve_Click" />
                        <asp:Button ID="RejectButton" runat="server" CssClass="btn btn-outline-danger" Text="Send back" OnClick="Reject_Click" />
                    </div>
                </asp:PlaceHolder>
            </div>
        </div>
    </asp:PlaceHolder>
    <div class="panel">
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Tutor</th><th>Week of</th><th class="col-num">Hours</th><th class="col-num">Entries</th><th>Submitted</th><th>Status</th><th class="col-action"></th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Tutor"><%#: Eval("TutorName") %></td>
                                <td data-label="Week of"><%# Fmt.Date((DateTime)Eval("WeekStart")) %></td>
                                <td data-label="Hours" class="col-num"><%# Fmt.Hours((decimal)Eval("TotalHours")) %></td>
                                <td data-label="Entries" class="col-num"><%# Eval("Entries") %></td>
                                <td data-label="Submitted"><%# Fmt.DateTime((DateTime?)Eval("SubmittedAt")) %></td>
                                <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %></td>
                                <td class="col-action"><a class="btn btn-sm btn-outline-primary" href="<%# Ui.WithQuery(Request, "id", Eval("TimesheetId").ToString()) %>">Open</a></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="EmptyState" runat="server" />
    </div>
</asp:Content>
