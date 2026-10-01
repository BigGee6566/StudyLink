<%@ Page Title="Book a Session" Language="C#" AutoEventWireup="true" CodeBehind="Tutors.aspx.cs" Inherits="StudyLink.Web.Catalogue.Tutors" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC7 - Book a Session: find a verified tutor by subject, level and campus --%>
    <div class="panel">
        <div class="panel__body">
            <asp:Panel runat="server" CssClass="filter-bar" DefaultButton="SearchButton">
                <div>
                    <label class="form-label" for="<%= Subject.ClientID %>">Subject</label>
                    <asp:DropDownList ID="Subject" runat="server" CssClass="form-select" />
                </div>
                <div>
                    <label class="form-label" for="<%= Level.ClientID %>">Level</label>
                    <asp:DropDownList ID="Level" runat="server" CssClass="form-select" />
                </div>
                <div>
                    <label class="form-label" for="<%= Campus.ClientID %>">Campus</label>
                    <asp:DropDownList ID="Campus" runat="server" CssClass="form-select" />
                </div>
                <div>
                    <label class="form-label" for="<%= SearchText.ClientID %>">Tutor name</label>
                    <asp:TextBox ID="SearchText" runat="server" CssClass="form-control" MaxLength="60" />
                </div>
                <asp:Button ID="SearchButton" runat="server" CssClass="btn btn-primary" Text="Find tutors" OnClick="Search_Click" />
            </asp:Panel>
        </div>
    </div>

    <p class="muted"><asp:Literal ID="ResultSummary" runat="server" /></p>
    <div class="tutor-grid">
        <asp:Repeater ID="TutorList" runat="server">
            <ItemTemplate>
                <div class="tutor-card">
                    <div class="tutor-card__head">
                        <div class="avatar" aria-hidden="true"><%#: Eval("Initials") %></div>
                        <div>
                            <div class="tutor-card__name"><%#: Eval("FullName") %></div>
                            <div class="muted small"><%#: Eval("Qualification") %> · <%# Eval("ExperienceYears") %> yrs</div>
                        </div>
                    </div>
                    <div><%# Ui.Stars((decimal?)Eval("AverageRating"), (int)Eval("ReviewCount")) %></div>
                    <div class="chips"><%# Chips((string)Eval("Subjects")) %></div>
                    <div class="d-flex justify-content-between small">
                        <span><%# Icons.Svg("pin", 14) %> <%#: Eval("Campus") %></span>
                        <span class="fw-semibold"><%# Fmt.Money(Eval("HourlyRate")) %>/hour</span>
                    </div>
                    <div class="small muted"><%# NextFree((DateTime?)Eval("NextAvailable")) %></div>
                    <div class="d-flex gap-2 mt-auto">
                        <a class="btn btn-outline-primary flex-fill" href="<%# ResolveUrl("~/Catalogue/TutorProfile.aspx?id=" + Eval("TutorId")) %>">Profile</a>
                        <a class="btn btn-primary flex-fill" href="<%# BookUrl((int)Eval("TutorId")) %>">Book</a>
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>
    <asp:Literal ID="EmptyState" runat="server" />
    <asp:Literal ID="Pager" runat="server" />
</asp:Content>