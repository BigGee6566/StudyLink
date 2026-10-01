<%@ Page Title="Availability" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Availability.aspx.cs" Inherits="StudyLink.Web.Tutor.Availability" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:Literal ID="VerifyNote" runat="server" />
    <div class="detail-grid">
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title">Published slots</h2><span class="muted small">From today</span></div>
            <div class="table-wrap">
                <table class="data-table stack">
                    <thead><tr><th>Date</th><th>Time</th><th>Subject</th><th>Campus</th><th class="col-num">Fee</th><th>Booking</th><th class="col-action"></th></tr></thead>
                    <tbody>
                        <asp:Repeater ID="Slots" runat="server" OnItemCommand="Slot_Command">
                            <ItemTemplate>
                                <tr>
                                    <td data-label="Date"><%# Fmt.Day((DateTime)Eval("Date")) %></td>
                                    <td data-label="Time"><%#: Eval("TimeRange") %></td>
                                    <td data-label="Subject"><%#: Eval("Subject") %><div class="muted small"><%#: Eval("StudyLevel") %></div></td>
                                    <td data-label="Campus"><%#: Eval("Campus") %></td>
                                    <td data-label="Fee" class="col-num"><%# Fmt.Money(Eval("Fee")) %></td>
                                    <td data-label="Booking"><%# (bool)Eval("IsBooked") ? Fmt.H(Eval("BookedBy")) + "<div>" + Fmt.Badge(Eval("BookingStatus")) + "</div>" : "<span class=\"badge badge--success\">Open</span>" %></td>
                                    <td class="col-action"><asp:LinkButton runat="server" CssClass="btn btn-sm btn-link btn-link-danger" CommandName="delete" CommandArgument='<%# Eval("AvailabilityId") %>'
                                        Visible='<%# !(bool)Eval("IsBooked") %>' data-confirm="Remove this open slot?">Remove</asp:LinkButton></td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
            <asp:Literal ID="NoSlots" runat="server" />
        </div>
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title">Add a slot</h2></div>
            <div class="panel__body">
                <div class="mb-2"><label class="form-label" for="<%= TutorSubject.ClientID %>">Subject</label><asp:DropDownList ID="TutorSubject" runat="server" CssClass="form-select" /></div>
                <div class="mb-2"><label class="form-label" for="<%= Campus.ClientID %>">Campus</label><asp:DropDownList ID="Campus" runat="server" CssClass="form-select" /></div>
                <div class="mb-2"><label class="form-label" for="<%= SlotDate.ClientID %>">Date</label><asp:TextBox ID="SlotDate" runat="server" CssClass="form-control" TextMode="Date" /></div>
                <div class="row g-2 mb-2">
                    <div class="col"><label class="form-label" for="<%= Start.ClientID %>">Start</label><asp:TextBox ID="Start" runat="server" CssClass="form-control" TextMode="Time" /></div>
                    <div class="col"><label class="form-label" for="<%= EndTime.ClientID %>">End</label><asp:TextBox ID="EndTime" runat="server" CssClass="form-control" TextMode="Time" /></div>
                </div>
                <div class="mb-3"><label class="form-label" for="<%= Repeat.ClientID %>">Repeat weekly</label>
                    <asp:DropDownList ID="Repeat" runat="server" CssClass="form-select">
                        <asp:ListItem Value="0">Just this date</asp:ListItem><asp:ListItem Value="3">For 4 weeks</asp:ListItem>
                        <asp:ListItem Value="7">For 8 weeks</asp:ListItem><asp:ListItem Value="11">For 12 weeks</asp:ListItem>
                    </asp:DropDownList></div>
                <asp:Button ID="AddButton" runat="server" CssClass="btn btn-primary btn-block" Text="Publish slot" OnClick="Add_Click" />
                <p class="form-hint mt-2">Slots must be in the future, at least 30 minutes long, within the hub's maximum session length, and cannot overlap your other slots.</p>
            </div>
        </div>
    </div>
</asp:Content>