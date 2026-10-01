<%@ Page Title="Bringing students and tutors together" Language="C#" MasterPageFile="~/MasterPages/Public.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="StudyLink.Web.Default" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC1 - Home / Welcome (Landing Page) --%>
    <section class="landing-hero">
        <div class="landing-hero__inner">
            <div>
                <div class="landing-logo">
                    <img src="<%: ResolveUrl("~/Images/branding/studylink-logo.png") %>" alt="StudyLink logo" />
                </div>
                <h1>Bringing students and tutors together.</h1>
                <p class="lead">StudyLink is Forte Books &amp; Tuition Hub's Books, Tutoring &amp; Academic Resource Hub. Book verified tutors, rent or buy prescribed textbooks and pay your fees in one place.</p>
                <div class="d-flex flex-wrap gap-2 mt-4">
                    <a class="btn btn-accent btn-lg" href="<%: ResolveUrl("~/Account/Login.aspx") %>">Log in</a>
                    <a class="btn btn-light btn-lg" href="<%: ResolveUrl("~/Account/Register.aspx?role=student") %>">Register as Student</a>
                    <a class="btn btn-light-outline btn-lg" href="<%: ResolveUrl("~/Account/Register.aspx?role=tutor") %>">Register as Tutor</a>
                </div>
            </div>
            <div class="landing-card">
                <h2>Choose your campus</h2>
                <p class="muted mb-0">We'll show you the books on the shelf and the tutors working at your campus.</p>
                <div class="campus-picker" role="radiogroup" aria-label="Campus">
                    <asp:Repeater ID="CampusList" runat="server">
                        <ItemTemplate>
                            <label><input type="radio" name="campus" value="<%# Eval("Id") %>" <%# (int)Eval("Id") == SelectedCampus ? "checked=\"checked\"" : "" %> /><span><%#: Eval("Name") %></span></label>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
                <div class="d-grid gap-2">
                    <asp:Button ID="BrowseBooksButton" runat="server" CssClass="btn btn-primary" Text="Browse books at this campus" OnClick="BrowseBooks_Click" />
                    <asp:Button ID="FindTutorButton" runat="server" CssClass="btn btn-outline-primary" Text="Find a tutor at this campus" OnClick="FindTutor_Click" />
                </div>
                <hr />
                <div class="d-flex justify-content-between text-center small">
                    <div><div class="fs-4 fw-bold text-dark"><asp:Literal ID="BookCount" runat="server" /></div>titles</div>
                    <div><div class="fs-4 fw-bold text-dark"><asp:Literal ID="TutorCount" runat="server" /></div>verified tutors</div>
                    <div><div class="fs-4 fw-bold text-dark">3</div>campuses</div>
                </div>
            </div>
        </div>
    </section>

    <nav class="quick-links" aria-label="Quick links">
        <a class="quick-link" href="<%: ResolveUrl("~/Catalogue/Books.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("book") %></span><strong>Browse Books</strong><span>Prescribed textbooks to rent or buy, with live stock per campus.</span></a>
        <a class="quick-link" href="<%: ResolveUrl("~/Catalogue/Tutors.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("cap") %></span><strong>Find a Tutor</strong><span>Verified tutors by subject and level, with real-time availability.</span></a>
        <a class="quick-link" href="<%: ResolveUrl("~/Public/HowItWorks.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("info") %></span><strong>How It Works</strong><span>From registering to paying your fees, step by step.</span></a>
        <a class="quick-link" href="<%: ResolveUrl("~/Public/Contact.aspx") %>"><span class="action-card__icon"><%= Icons.Svg("support") %></span><strong>Contact / Support</strong><span>Questions about books, sessions or your account.</span></a>
    </nav>

    <section class="landing-section">
        <h2>How StudyLink works</h2>
        <div class="steps">
            <div class="step"><div class="step__num">1</div><strong>Register</strong><p class="muted mb-0">Students start straight away. Tutors are verified by hub staff before they can be booked.</p></div>
            <div class="step"><div class="step__num">2</div><strong>Book or rent</strong><p class="muted mb-0">Pick a tutor's open slot or reserve a textbook at your campus. Slots can never be double-booked.</p></div>
            <div class="step"><div class="step__num">3</div><strong>Pay securely</strong><p class="muted mb-0">Pay by card and get an instant receipt. Every fee is on your statement.</p></div>
            <div class="step"><div class="step__num">4</div><strong>Learn</strong><p class="muted mb-0">Get reminders before sessions and due dates, then rate your tutor.</p></div>
        </div>
    </section>
</asp:Content>