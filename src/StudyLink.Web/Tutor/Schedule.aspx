<%@ Page Title="Schedule" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Schedule.aspx.cs" Inherits="StudyLink.Web.Tutor.Schedule" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:Literal ID="TabStrip" runat="server" />
    <asp:PlaceHolder ID="PostponePanel" runat="server">
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title">Postponement requests</h2></div>
            <div class="table-wrap">
                <table class="data-table stack">
                    <thead><tr><th>Student</th><th>Session</th><th>Currently</th><th>Proposed</th><th>Reason</th><th class="col-action"></th></tr></thead>
                    <tbody>
                        <asp:Repeater ID="Postponements" runat="server" OnItemCommand="Postpone_Command">
                            <ItemTemplate>
                                <tr>
                                    <td data-label="Student"><%#: Eval("StudentName") %></td>
                                    <td data-label="Session"><%#: Eval("Subject") %><div class="muted small"><%#: Eval("BookingReference") %></div></td>
                                    <td data-label="Currently"><%# Fmt.Day((DateTime)Eval("OriginalDate")) %>, <%# Fmt.Range((TimeSpan)Eval("OriginalStart"), (TimeSpan)Eval("OriginalEnd")) %></td>
                                    <td data-label="Proposed"><strong><%# Fmt.Day((DateTime)Eval("ProposedDate")) %>, <%# Fmt.Range((TimeSpan)Eval("ProposedStart"), (TimeSpan)Eval("ProposedEnd")) %></strong></td>
                                    <td data-label="Reason"><%#: Fmt.Words(Eval("ReasonCategory")) %><div class="muted small"><%#: Eval("StudentNote") %></div></td>
                                    <td class="col-action">
                                        <asp:LinkButton runat="server" CssClass="btn btn-sm btn-primary" CommandName="approve" CommandArgument='<%# Eval("PostponementId") %>'>Approve</asp:LinkButton>
                                        <asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-danger" CommandName="decline" CommandArgument='<%# Eval("PostponementId") %>' data-confirm="Decline this request? The session stays at its current time.">Decline</asp:LinkButton>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
        </div>
    </asp:PlaceHolder>
    <div class="panel">
        <div class="panel__header"><h2 class="panel__title"><asp:Literal ID="ListTitle" runat="server" /></h2></div>
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>When</th><th>Student</th><th>Session</th><th class="col-num">Fee</th><th>Status</th><th class="col-action"></th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="When"><%# Fmt.Day((DateTime)Eval("SessionDate")) %><div class="muted small"><%#: Eval("TimeRange") %></div></td>
                                <td data-label="Student"><%#: Eval("StudentName") %><div class="muted small"><%#: Eval("StudentNumber") %></div></td>
                                <td data-label="Session"><%#: Eval("Subject") %><div class="muted small"><%#: Eval("StudyLevel") %> · <%#: Eval("Campus") %></div></td>
                                <td data-label="Fee" class="col-num"><%# Fmt.Money(Eval("Fee")) %></td>
                                <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %></td>
                                <td class="col-action"><a class="btn btn-sm btn-outline-primary" href="<%# ResolveUrl("~/Tutor/SessionDetails.aspx?id=" + Eval("BookingId")) %>">Open</a></td>
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