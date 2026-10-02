<%@ Page Title="My Bookings" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="MyBookings.aspx.cs" Inherits="StudyLink.Web.Student.Tutoring.MyBookings" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="panel">
        <div class="panel__body pb-0"><asp:Literal ID="TabStrip" runat="server" /></div>
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Reference</th><th>Session</th><th>Tutor</th><th>When</th><th class="col-num">Fee</th><th>Status</th><th class="col-action"></th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Reference" class="numeric"><%#: Eval("Reference") %></td>
                                <td data-label="Session"><span class="d-inline-flex align-items-center gap-2"><%# Icons.Subject((string)Eval("Subject"), 16) %><%#: Eval("Subject") %></span><div class="muted small"><%#: Eval("StudyLevel") %> · <%#: Eval("Campus") %></div></td>
                                <td data-label="Tutor"><%#: Eval("TutorName") %></td>
                                <td data-label="When"><%# Fmt.Day((DateTime)Eval("SessionDate")) %><div class="muted small"><%#: Eval("TimeRange") %></div></td>
                                <td data-label="Fee" class="col-num"><%# Fmt.Money(Eval("Fee")) %></td>
                                <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %></td>
                                <td class="col-action"><a class="btn btn-sm btn-outline-primary" href="<%# ResolveUrl("~/Student/Tutoring/BookingDetails.aspx?id=" + Eval("BookingId")) %>">Details</a></td>
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