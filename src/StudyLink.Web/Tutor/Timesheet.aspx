<%@ Page Title="Timesheets" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Timesheet.aspx.cs" Inherits="StudyLink.Web.Tutor.Timesheet" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="d-flex justify-content-between align-items-center flex-wrap gap-2 mb-3">
        <div class="d-flex gap-2">
            <a class="btn btn-outline-primary btn-sm" id="PrevWeek" runat="server">&larr; Previous week</a>
            <a class="btn btn-outline-primary btn-sm" id="ThisWeek" runat="server">This week</a>
            <a class="btn btn-outline-primary btn-sm" id="NextWeek" runat="server">Next week &rarr;</a>
        </div>
        <div><asp:Literal ID="WeekStatus" runat="server" /></div>
    </div>
    <asp:Literal ID="ReviewNote" runat="server" />
    <div class="detail-grid">
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title"><asp:Literal ID="WeekTitle" runat="server" /></h2><span class="fw-bold"><asp:Literal ID="Total" runat="server" /></span></div>
            <div class="table-wrap">
                <table class="data-table stack">
                    <thead><tr><th>Date</th><th>Work</th><th class="col-num">Hours</th><th class="col-action"></th></tr></thead>
                    <tbody>
                        <asp:Repeater ID="Entries" runat="server" OnItemCommand="Entry_Command">
                            <ItemTemplate>
                                <tr>
                                    <td data-label="Date"><%# Fmt.Day((DateTime)Eval("WorkDate")) %></td>
                                    <td data-label="Work"><%#: Eval("Description") %><%# Eval("BookingReference") == null ? "" : "<div class=\"muted small\">Session " + Fmt.H(Eval("BookingReference")) + "</div>" %></td>
                                    <td data-label="Hours" class="col-num"><%# Fmt.Hours((decimal)Eval("Hours")) %></td>
                                    <td class="col-action"><asp:LinkButton runat="server" CssClass="btn btn-sm btn-link btn-link-danger" CommandName="remove" CommandArgument='<%# Eval("EntryId") %>'
                                        Visible='<%# CanEdit && Eval("BookingId") == null %>'>Remove</asp:LinkButton></td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
            <asp:Literal ID="NoEntries" runat="server" />
            <div class="panel__footer d-flex justify-content-end">
                <asp:Button ID="SubmitButton" runat="server" CssClass="btn btn-primary" Text="Submit timesheet" OnClick="Submit_Click" data-confirm="Submit this week's timesheet for approval? You can't edit it afterwards unless it is sent back." />
            </div>
        </div>
        <div>
            <asp:PlaceHolder ID="AddForm" runat="server">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title">Add other work</h2></div>
                    <div class="panel__body">
                        <div class="mb-2"><label class="form-label" for="<%= WorkDate.ClientID %>">Date</label><asp:TextBox ID="WorkDate" runat="server" CssClass="form-control" TextMode="Date" /></div>
                        <div class="mb-2"><label class="form-label" for="<%= Hours.ClientID %>">Hours</label><asp:TextBox ID="Hours" runat="server" CssClass="form-control" MaxLength="5" inputmode="decimal" /></div>
                        <div class="mb-3"><label class="form-label" for="<%= Description.ClientID %>">Description</label><asp:TextBox ID="Description" runat="server" CssClass="form-control" MaxLength="200" placeholder="e.g. Preparing exam revision pack" /></div>
                        <asp:Button ID="AddButton" runat="server" CssClass="btn btn-outline-primary btn-block" Text="Add entry" OnClick="Add_Click" />
                        <p class="form-hint mt-2">Attended sessions are added automatically when you complete them.</p>
                    </div>
                </div>
            </asp:PlaceHolder>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">History</h2></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="History" runat="server">
                            <ItemTemplate>
                                <a class="item" href="<%# ResolveUrl("~/Tutor/Timesheet.aspx?week=" + Ui.IsoDate((DateTime)Eval("WeekStart"))) %>">
                                    <div class="item__main"><div class="item__title">Week of <%# Fmt.Date((DateTime)Eval("WeekStart")) %></div><div class="item__meta"><%# Fmt.Hours((decimal)Eval("TotalHours")) %> · <%# Eval("Entries") %> entries</div></div>
                                    <div class="item__side"><%# Fmt.Badge(Eval("Status")) %></div>
                                </a>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>