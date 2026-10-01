<%@ Page Title="Session" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="SessionDetails.aspx.cs" Inherits="StudyLink.Web.Tutor.SessionDetails" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <p class="mb-3"><a href="<%: ResolveUrl("~/Tutor/Schedule.aspx") %>">&larr; Schedule</a></p>
    <div class="detail-grid">
        <div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Session</h2><asp:Literal ID="StatusBadge" runat="server" /></div>
                <div class="panel__body">
                    <dl class="fact-list">
                        <dt>Reference</dt><dd><asp:Literal ID="Reference" runat="server" /></dd>
                        <dt>Student</dt><dd><asp:Literal ID="Student" runat="server" /></dd>
                        <dt>Contact</dt><dd><asp:Literal ID="Contact" runat="server" /></dd>
                        <dt>Subject</dt><dd><asp:Literal ID="Subject" runat="server" /></dd>
                        <dt>When</dt><dd><asp:Literal ID="When" runat="server" /></dd>
                        <dt>Campus</dt><dd><asp:Literal ID="Campus" runat="server" /></dd>
                        <dt>Fee</dt><dd><asp:Literal ID="Fee" runat="server" /></dd>
                        <asp:Literal ID="ExtraFacts" runat="server" />
                    </dl>
                </div>
            </div>
            <asp:Literal ID="ReviewBox" runat="server" />
        </div>
        <div>
            <asp:PlaceHolder ID="RespondForm" runat="server">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title">Confirm this session</h2></div>
                    <div class="panel__body">
                        <p>The student has paid. Confirm to lock in the session, or decline to release the slot and refund them.</p>
                        <asp:Button ID="ConfirmButton" runat="server" CssClass="btn btn-primary btn-block mb-3" Text="Confirm session" OnClick="Confirm_Click" />
                        <label class="form-label" for="<%= DeclineReason.ClientID %>">Reason for declining</label>
                        <asp:TextBox ID="DeclineReason" runat="server" CssClass="form-control mb-2" MaxLength="300" />
                        <asp:Button ID="DeclineButton" runat="server" CssClass="btn btn-outline-danger btn-block" Text="Decline and refund" OnClick="Decline_Click" data-confirm="Decline this session? The student will be refunded in full." />
                    </div>
                </div>
            </asp:PlaceHolder>
            <asp:PlaceHolder ID="CompleteForm" runat="server">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title">Attendance</h2></div>
                    <div class="panel__body">
                        <label class="form-label" for="<%= Attendance.ClientID %>">What happened?</label>
                        <asp:DropDownList ID="Attendance" runat="server" CssClass="form-select mb-2" />
                        <label class="form-label" for="<%= Notes.ClientID %>">Session notes</label>
                        <asp:TextBox ID="Notes" runat="server" CssClass="form-control mb-3" TextMode="MultiLine" Rows="3" MaxLength="1000" data-max="1000" />
                        <asp:Button ID="CompleteButton" runat="server" CssClass="btn btn-primary btn-block" Text="Complete session" OnClick="Complete_Click" />
                        <p class="form-hint mt-2">Attended sessions are added to your weekly timesheet automatically.</p>
                    </div>
                </div>
            </asp:PlaceHolder>
            <asp:PlaceHolder ID="CancelForm" runat="server">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title">Cancel session</h2></div>
                    <div class="panel__body">
                        <asp:DropDownList ID="CancelReason" runat="server" CssClass="form-select mb-2" aria-label="Reason" />
                        <asp:TextBox ID="CancelNote" runat="server" CssClass="form-control mb-2" MaxLength="500" placeholder="Explain to the student" />
                        <asp:Button ID="CancelButton" runat="server" CssClass="btn btn-outline-danger btn-block" Text="Cancel and refund" OnClick="Cancel_Click" data-confirm="Cancel this session? The student will be refunded." />
                    </div>
                </div>
            </asp:PlaceHolder>
        </div>
    </div>
</asp:Content>