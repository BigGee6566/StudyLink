<%@ Page Title="Tutor profile" Language="C#" AutoEventWireup="true" CodeBehind="TutorProfile.aspx.cs" Inherits="StudyLink.Web.Catalogue.TutorProfile" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <p class="mb-3"><a href="<%: ResolveUrl("~/Catalogue/Tutors.aspx") %>">&larr; Back to tutors</a></p>
    <div class="detail-grid">
        <div>
            <div class="panel">
                <div class="panel__body">
                    <div class="tutor-card__head mb-3">
                        <div class="avatar" aria-hidden="true"><asp:Literal ID="Initials" runat="server" /></div>
                        <div>
                            <div class="tutor-card__name fs-5"><asp:Literal ID="FullName" runat="server" /></div>
                            <div class="muted"><asp:Literal ID="Qualification" runat="server" /></div>
                            <div><asp:Literal ID="Rating" runat="server" /></div>
                        </div>
                    </div>
                    <p><asp:Literal ID="Bio" runat="server" /></p>
                    <h3 class="section-title">Subjects</h3>
                    <div class="chips"><asp:Literal ID="SubjectChips" runat="server" /></div>
                </div>
            </div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Reviews</h2></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="ReviewList" runat="server">
                            <ItemTemplate>
                                <div class="item">
                                    <div class="item__main">
                                        <div><%# Ui.Stars((byte)Eval("Rating")) %></div>
                                        <div class="mt-1"><%#: Eval("Comment") %></div>
                                        <div class="item__meta"><%#: Eval("Subject") %> · <%# Fmt.Date((DateTime)Eval("CreatedAt")) %></div>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoReviews" runat="server" />
                </div>
            </div>
        </div>
        <div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Rates &amp; availability</h2></div>
                <div class="panel__body">
                    <dl class="fact-list mb-3">
                        <dt>Rate</dt><dd><asp:Literal ID="Rate" runat="server" /></dd>
                        <dt>Campus</dt><dd><asp:Literal ID="CampusName" runat="server" /></dd>
                        <dt>Experience</dt><dd><asp:Literal ID="Experience" runat="server" /></dd>
                    </dl>
                    <h3 class="section-title mt-0">Next open slots</h3>
                    <div class="item-list mb-3">
                        <asp:Repeater ID="SlotList" runat="server">
                            <ItemTemplate>
                                <div class="item">
                                    <div class="item__main">
                                        <div class="item__title"><%# Fmt.Day((DateTime)Eval("Date")) %> · <%#: Eval("TimeRange") %></div>
                                        <div class="item__meta"><%#: Eval("Subject") %> · <%#: Eval("Campus") %></div>
                                    </div>
                                    <div class="item__side fw-semibold"><%# Fmt.Money(Eval("Fee")) %></div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="NoSlots" runat="server" />
                    <a class="btn btn-primary btn-block" id="BookLink" runat="server">Choose a time slot</a>
                </div>
            </div>
        </div>
    </div>
</asp:Content>