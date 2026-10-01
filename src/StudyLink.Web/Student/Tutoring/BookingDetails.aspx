<%@ Page Title="Booking details" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="BookingDetails.aspx.cs" Inherits="StudyLink.Web.Student.Tutoring.BookingDetails" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <p class="mb-3"><a href="<%: ResolveUrl("~/Student/Tutoring/MyBookings.aspx") %>">&larr; My bookings</a></p>
    <div class="detail-grid">
        <div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Session</h2><asp:Literal ID="StatusBadge" runat="server" /></div>
                <div class="panel__body">
                    <dl class="fact-list">
                        <dt>Reference</dt><dd><asp:Literal ID="Reference" runat="server" /></dd>
                        <dt>Tutor</dt><dd><asp:Literal ID="Tutor" runat="server" /></dd>
                        <dt>Subject</dt><dd><asp:Literal ID="Subject" runat="server" /></dd>
                        <dt>When</dt><dd><asp:Literal ID="When" runat="server" /></dd>
                        <dt>Campus</dt><dd><asp:Literal ID="Campus" runat="server" /></dd>
                        <dt>Fee</dt><dd><asp:Literal ID="Fee" runat="server" /></dd>
                        <asp:Literal ID="ExtraFacts" runat="server" />
                    </dl>
                </div>
            </div>

            <asp:PlaceHolder ID="ReviewShown" runat="server">
                <div class="panel"><div class="panel__header"><h2 class="panel__title">Your review</h2></div>
                    <div class="panel__body"><asp:Literal ID="ReviewText" runat="server" /></div></div>
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="ReviewForm" runat="server">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title">Rate this session</h2></div>
                    <div class="panel__body">
                        <asp:RadioButtonList ID="Rating" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" CssClass="d-flex gap-3 mb-3">
                            <asp:ListItem Value="5">★★★★★</asp:ListItem><asp:ListItem Value="4">★★★★</asp:ListItem><asp:ListItem Value="3">★★★</asp:ListItem>
                            <asp:ListItem Value="2">★★</asp:ListItem><asp:ListItem Value="1">★</asp:ListItem>
                        </asp:RadioButtonList>
                        <label class="form-label" for="<%= ReviewComment.ClientID %>">Comment</label>
                        <asp:TextBox ID="ReviewComment" runat="server" CssClass="form-control mb-3" TextMode="MultiLine" Rows="3" MaxLength="500" data-max="500" />
                        <asp:Button ID="ReviewButton" runat="server" CssClass="btn btn-primary" Text="Submit review" OnClick="Review_Click" />
                    </div>
                </div>
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="PostponeHistory" runat="server">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title">Postponement requests</h2></div>
                    <div class="table-wrap">
                        <table class="data-table stack">
                            <thead><tr><th>Requested</th><th>Proposed time</th><th>Reason</th><th>Status</th></tr></thead>
                            <tbody>
                                <asp:Repeater ID="Postponements" runat="server">
                                    <ItemTemplate>
                                        <tr>
                                            <td data-label="Requested"><%# Fmt.DateTime((DateTime)Eval("CreatedAt")) %></td>
                                            <td data-label="Proposed"><%# Fmt.Day((DateTime)Eval("ProposedDate")) %>, <%# Fmt.Range((TimeSpan)Eval("ProposedStart"), (TimeSpan)Eval("ProposedEnd")) %></td>
                                            <td data-label="Reason"><%#: Fmt.Words(Eval("ReasonCategory")) %><div class="muted small"><%#: Eval("TutorResponseNote") %></div></td>
                                            <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                </div>
            </asp:PlaceHolder>
        </div>

        <div>
            <asp:PlaceHolder ID="PayBox" runat="server">
                <div class="panel"><div class="panel__body">
                    <p class="mb-2">This slot is held until <strong><asp:Literal ID="HoldUntil" runat="server" /></strong>. Pay to send it to the tutor.</p>
                    <a class="btn btn-accent btn-block" id="PayLink" runat="server">Pay now</a>
                </div></div>
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="PendingPostpone" runat="server">
                <div class="panel"><div class="panel__header"><h2 class="panel__title">Postponement pending</h2></div>
                    <div class="panel__body">
                        <p><asp:Literal ID="PendingText" runat="server" /></p>
                        <asp:Button ID="WithdrawButton" runat="server" CssClass="btn btn-outline-primary btn-sm" Text="Withdraw request" OnClick="Withdraw_Click" data-confirm="Withdraw this postponement request?" />
                    </div></div>
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="PostponeForm" runat="server">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title">Ask to postpone</h2></div>
                    <div class="panel__body">
                        <div class="mb-2"><label class="form-label" for="<%= NewDate.ClientID %>">New date</label><asp:TextBox ID="NewDate" runat="server" CssClass="form-control" TextMode="Date" /></div>
                        <div class="row g-2 mb-2">
                            <div class="col"><label class="form-label" for="<%= NewStart.ClientID %>">Start</label><asp:TextBox ID="NewStart" runat="server" CssClass="form-control" TextMode="Time" /></div>
                            <div class="col"><label class="form-label" for="<%= NewEnd.ClientID %>">End</label><asp:TextBox ID="NewEnd" runat="server" CssClass="form-control" TextMode="Time" /></div>
                        </div>
                        <div class="mb-2"><label class="form-label" for="<%= PostponeReason.ClientID %>">Reason</label><asp:DropDownList ID="PostponeReason" runat="server" CssClass="form-select" /></div>
                        <div class="mb-3"><label class="form-label" for="<%= PostponeNote.ClientID %>">Note to tutor</label><asp:TextBox ID="PostponeNote" runat="server" CssClass="form-control" MaxLength="500" /></div>
                        <asp:Button ID="PostponeButton" runat="server" CssClass="btn btn-outline-primary btn-block" Text="Send request" OnClick="Postpone_Click" />
                    </div>
                </div>
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="CancelForm" runat="server">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title">Cancel session</h2></div>
                    <div class="panel__body">
                        <div class="mb-2"><label class="form-label" for="<%= CancelReason.ClientID %>">Reason</label><asp:DropDownList ID="CancelReason" runat="server" CssClass="form-select" /></div>
                        <div class="mb-3"><label class="form-label" for="<%= CancelNote.ClientID %>">Details</label><asp:TextBox ID="CancelNote" runat="server" CssClass="form-control" MaxLength="500" /></div>
                        <asp:Button ID="CancelButton" runat="server" CssClass="btn btn-outline-danger btn-block" Text="Cancel this session" OnClick="Cancel_Click" data-confirm="Cancel this session?" />
                        <p class="form-hint mt-2"><asp:Literal ID="CancelPolicy" runat="server" /></p>
                    </div>
                </div>
            </asp:PlaceHolder>
            <asp:Literal ID="CancelBlocked" runat="server" />
        </div>
    </div>
</asp:Content>