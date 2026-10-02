<%@ Page Title="Date & Time Slot" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="SelectSlot.aspx.cs" Inherits="StudyLink.Web.Student.Tutoring.SelectSlot" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC8 - Date & Time Slot: only open, future slots of a verified tutor are offered --%>
    <p class="mb-3"><a href="<%: ResolveUrl("~/Catalogue/Tutors.aspx") %>">&larr; Choose a different tutor</a></p>
    <div class="detail-grid">
        <div>
            <div class="panel">
                <div class="panel__header">
                    <h2 class="panel__title">1. Pick a date</h2>
                    <asp:DropDownList ID="Subject" runat="server" CssClass="form-select form-select-sm w-auto" AutoPostBack="true" OnSelectedIndexChanged="Subject_Changed" aria-label="Subject" />
                </div>
                <div class="panel__body">
                    <div class="date-strip">
                        <asp:Repeater ID="Dates" runat="server">
                            <ItemTemplate>
                                <a class="date-pill" href="<%# DateUrl((DateTime)Container.DataItem) %>" <%# IsSelected((DateTime)Container.DataItem) ? "aria-current=\"date\"" : "" %>>
                                    <small><%# ((DateTime)Container.DataItem).ToString("ddd") %></small>
                                    <strong><%# ((DateTime)Container.DataItem).Day %></strong>
                                    <small><%# ((DateTime)Container.DataItem).ToString("MMM") %></small>
                                </a>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoDates" runat="server" />
                </div>
            </div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">2. Pick a time</h2><span class="muted small"><asp:Literal ID="DateLabel" runat="server" /></span></div>
                <div class="panel__body">
                    <div class="slot-grid" data-slot-target="<%= SlotId.ClientID %>">
                        <asp:Repeater ID="Slots" runat="server">
                            <ItemTemplate>
                                <button type="button" class="slot-btn" aria-pressed="false" data-slot="<%# Eval("AvailabilityId") %>"
                                        data-time="<%#: Fmt.Day((DateTime)Eval("Date")) + ", " + Eval("TimeRange") %>" data-fee="<%#: Fmt.Money(Eval("Fee")) %>"
                                        data-hours="<%#: Fmt.Hours((decimal)Eval("Hours")) %>" data-campus="<%#: Eval("Campus") %>" data-subject="<%#: Eval("Subject") + " · " + Eval("StudyLevel") %>">
                                    <%#: Eval("TimeRange") %><small><%# Icons.Subject((string)Eval("Subject"), 11) %> <%#: Eval("Subject") %></small>
                                </button>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:HiddenField ID="SlotId" runat="server" />
                    <asp:Literal ID="NoSlots" runat="server" />
                </div>
            </div>
        </div>
        <div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Your booking</h2></div>
                <div class="panel__body">
                    <div class="tutor-card__head mb-3">
                        <asp:Literal ID="TutorAvatar" runat="server" />
                        <div><div class="tutor-card__name"><asp:Literal ID="TutorName" runat="server" /></div><div class="muted small"><asp:Literal ID="TutorRate" runat="server" /></div></div>
                    </div>
                    <div class="summary-box mb-3" data-slot-summary="true" hidden="hidden">
                        <dl class="fact-list">
                            <dt>When</dt><dd data-field="time"></dd>
                            <dt>Subject</dt><dd data-field="subject"></dd>
                            <dt>Campus</dt><dd data-field="campus"></dd>
                            <dt>Duration</dt><dd data-field="hours"></dd>
                        </dl>
                        <div class="summary-total"><span>Session fee</span><span data-field="fee"></span></div>
                    </div>
                    <label class="form-label" for="<%= Message.ClientID %>">Message for the tutor <span class="muted fw-normal">(optional)</span></label>
                    <asp:TextBox ID="Message" runat="server" CssClass="form-control mb-3" TextMode="MultiLine" Rows="3" MaxLength="500" data-max="500" placeholder="What would you like to work on?" />
                    <asp:Button ID="ContinueButton" runat="server" CssClass="btn btn-primary btn-lg btn-block" Text="Continue to payment" OnClick="Continue_Click" data-slot-submit="true" data-once="Reserving your slot…" />
                    <p class="form-hint mt-2">The slot is held for you while you pay. The tutor then confirms the session; if they decline you are refunded in full.</p>
                </div>
            </div>
        </div>
    </div>
</asp:Content>