<%@ Page Title="Bringing students and tutors together" Language="C#" MasterPageFile="~/MasterPages/Public.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="StudyLink.Web.Default" %>
<%@ Register TagPrefix="sl" TagName="HowItWorksSteps" Src="~/UserControls/HowItWorksSteps.ascx" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC1 - Home / Welcome (Landing Page) --%>
    <section class="hero">
        <div class="hero__media"><%= SiteImage.Img("hero", "University of Fort Hare students working together around a study table", "100vw", eager: true) %></div>
        <div class="hero__inner">
            <div>
                <span class="hero__eyebrow"><%= Icons.Svg("cap", 16) %> Forte Books &amp; Tuition Hub &middot; University of Fort Hare</span>
                <h1>Bringing students and tutors together.</h1>
                <p class="hero__lead">StudyLink is the hub's online service for the Alice, East London and Bisho campuses. Book a verified tutor, rent or buy your prescribed textbooks and pay your fees, all in one place.</p>
                <div class="hero__actions">
                    <a class="btn btn-accent btn-lg" href="<%: ResolveUrl("~/Account/Register.aspx?role=student") %>">Register as a student</a>
                    <a class="btn btn-light-outline btn-lg" href="<%: ResolveUrl("~/Account/Register.aspx?role=tutor") %>">Apply to tutor</a>
                </div>
                <p class="hero__login">Already have an account? <a href="<%: ResolveUrl("~/Account/Login.aspx") %>">Log in</a></p>
                <ul class="hero__trust">
                    <li><%= Icons.Svg("shield", 18) %> Tutors verified by hub staff</li>
                    <li><%= Icons.Svg("book", 18) %> Live stock at each campus</li>
                    <li><%= Icons.Svg("lock", 18) %> Secure card payments</li>
                </ul>
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
                    <asp:Button ID="FindTutorButton" runat="server" CssClass="btn btn-white" Text="Find a tutor at this campus" OnClick="FindTutor_Click" />
                </div>
                <div class="landing-stats">
                    <div><strong><asp:Literal ID="BookCount" runat="server" /></strong><span>textbook titles</span></div>
                    <div><strong><asp:Literal ID="TutorCount" runat="server" /></strong><span>verified tutors</span></div>
                    <div><strong>3</strong><span>campuses</span></div>
                </div>
            </div>
        </div>
    </section>

    <section class="section">
        <div class="section__inner">
            <div class="section__head">
                <div class="eyebrow">What StudyLink is</div>
                <h2>Everything you need to study, without the queue</h2>
                <p class="section__lead">Forte Books &amp; Tuition Hub supplies prescribed textbooks and tutoring at three University of Fort Hare campuses. StudyLink puts that service online, so you can check whether a book is on the shelf or a tutor is free before you make the trip.</p>
            </div>
            <div class="why-grid">
                <div class="why" data-reveal-item>
                    <span class="tile-icon"><%= Icons.Svg("cap", 24) %></span>
                    <h3>Tutoring that fits your timetable</h3>
                    <p>See each tutor's open slots and book one in a minute. Every tutor is checked by hub staff before students can book them.</p>
                </div>
                <div class="why" data-reveal-item>
                    <span class="tile-icon"><%= Icons.Svg("book", 24) %></span>
                    <h3>The right textbook at the right campus</h3>
                    <p>Search by title, author or ISBN and see how many copies are on the shelf at your campus, then rent it for the week or buy it.</p>
                </div>
                <div class="why" data-reveal-item>
                    <span class="tile-icon"><%= Icons.Svg("card", 24) %></span>
                    <h3>One statement for everything you owe</h3>
                    <p>Session fees, rentals, purchases and late fees appear in one place. Pay by card and keep every receipt.</p>
                </div>
            </div>

            <div class="section__head section__head--center mt-5 mb-0">
                <div class="eyebrow">Who uses StudyLink</div>
            </div>
            <div class="role-grid mt-3">
                <div class="role-card"><span class="tile-icon"><%= Icons.Svg("cap", 22) %></span><div><h3>Students</h3><p>Book tutors, rent or buy prescribed books and pay fees online.</p></div></div>
                <div class="role-card"><span class="tile-icon"><%= Icons.Svg("users", 22) %></span><div><h3>Tutors</h3><p>Publish your availability, confirm sessions and track your earnings.</p></div></div>
                <div class="role-card"><span class="tile-icon"><%= Icons.Svg("shield", 22) %></span><div><h3>Hub staff</h3><p>Verify tutors, process returns and fines, and help students at the counter.</p></div></div>
                <div class="role-card"><span class="tile-icon"><%= Icons.Svg("chart", 22) %></span><div><h3>Managers</h3><p>Run the catalogue, stock, payments and reports for all three campuses.</p></div></div>
                <div class="role-card"><span class="tile-icon"><%= Icons.Svg("truck", 22) %></span><div><h3>Suppliers</h3><p>Receive purchase orders, dispatch deliveries and submit invoices.</p></div></div>
                <div class="role-card"><span class="tile-icon"><%= Icons.Svg("settings", 22) %></span><div><h3>System admins</h3><p>Manage accounts, roles, security, backups and the audit trail.</p></div></div>
            </div>
        </div>
    </section>

    <section class="section section--alt">
        <div class="section__inner split">
            <div class="split__media">
                <%= SiteImage.Img("tutoring", "A student working through highlighted notes during a one-on-one tutoring session", "(max-width: 1023px) 100vw, 560px") %>
                <div class="split__inset"><%= SiteImage.Img("online", "A student in an online tutoring session on a laptop", "260px") %></div>
            </div>
            <div>
                <div class="eyebrow">Tutoring</div>
                <h2>Book a verified tutor in minutes</h2>
                <p class="section__lead">Get one-on-one help with the modules you find hardest, from a tutor who knows the work.</p>
                <ul class="checklist">
                    <li><%= Icons.Svg("check", 16) %><span><strong>Verified by hub staff.</strong> Identity and qualifications are checked before a tutor can be booked.</span></li>
                    <li><%= Icons.Svg("check", 16) %><span><strong>Real-time availability.</strong> Pick from the tutor's open slots; a slot can never be double-booked.</span></li>
                    <li><%= Icons.Svg("check", 16) %><span><strong>Pay to confirm.</strong> Your payment reserves the slot, and if the tutor declines you are refunded in full.</span></li>
                    <li><%= Icons.Svg("check", 16) %><span><strong>Honest reviews.</strong> Only students who attended a session can rate it.</span></li>
                </ul>
                <a class="btn btn-primary btn-lg" href="<%: ResolveUrl("~/Catalogue/Tutors.aspx") %>">Find a tutor</a>
            </div>
        </div>
    </section>

    <section class="section">
        <div class="section__inner split split--reverse">
            <div class="split__media">
                <%= SiteImage.Img("library", "Shelves of academic books in a university library", "(max-width: 1023px) 100vw, 560px") %>
                <div class="split__inset"><%= SiteImage.Img("book-pick", "A student taking a textbook from a bookshelf", "260px") %></div>
            </div>
            <div>
                <div class="eyebrow">Textbooks</div>
                <h2>Rent or buy your prescribed textbooks</h2>
                <p class="section__lead">A weekly rental costs a fraction of the shelf price, and you can see stock at your campus before you go.</p>
                <ul class="checklist">
                    <li><%= Icons.Svg("check", 16) %><span><strong>Live stock per campus.</strong> Copies are counted as they are rented, returned and sold.</span></li>
                    <li><%= Icons.Svg("check", 16) %><span><strong>Rent for <%: RentalDays %> days</strong> and renew up to <%: MaxRenewals %> times if nobody is waiting for the book.</span></li>
                    <li><%= Icons.Svg("check", 16) %><span><strong>Join the waitlist</strong> when a title is out, and we'll tell you when a copy comes back.</span></li>
                    <li><%= Icons.Svg("check", 16) %><span><strong>Buy outright</strong> when you need the book for the whole year.</span></li>
                </ul>
                <a class="btn btn-primary btn-lg" href="<%: ResolveUrl("~/Catalogue/Books.aspx") %>">Browse books</a>
            </div>
        </div>
    </section>

    <section class="section section--alt">
        <div class="section__inner">
            <div class="section__head section__head--center">
                <div class="eyebrow">How it works</div>
                <h2>From sign-up to your first session</h2>
                <p class="section__lead">Four steps, the same at every campus.</p>
            </div>
            <sl:HowItWorksSteps ID="Steps" runat="server" Audience="general" />
            <p class="text-center mt-5 mb-0"><a class="btn btn-white" href="<%: ResolveUrl("~/Public/HowItWorks.aspx") %>">Read the full guide</a></p>
        </div>
    </section>

    <section class="section">
        <div class="section__inner">
            <div class="section__head">
                <div class="eyebrow">Our campuses</div>
                <h2>At the hub counter on all three campuses</h2>
                <p class="section__lead">Collect and return books, get help with your account and pay at the counter. Choose a campus to see what is on its shelf.</p>
            </div>
            <div class="campus-grid">
                <% foreach (var c in CampusCards) { %>
                <a class="campus-card" href="<%: ResolveUrl("~/Catalogue/Books.aspx?campus=" + c.Id) %>">
                    <%= SiteImage.Img(c.Image, c.Name + " campus, University of Fort Hare", "(max-width: 767px) 100vw, 380px") %>
                    <div class="campus-card__body"><h3><%: c.Name %></h3><span>See books at <%: c.Name %> <%= Icons.Svg("arrow", 16) %></span></div>
                </a>
                <% } %>
            </div>
        </div>
    </section>

    <section class="section pt-0">
        <div class="section__inner">
            <div class="cta-band">
                <%= SiteImage.Img("community", "", "100vw") %>
                <div>
                    <h2>Ready to start?</h2>
                    <p>Join StudyLink with your student number, or apply to tutor at your campus.</p>
                </div>
                <div class="hero__actions mt-0">
                    <a class="btn btn-accent btn-lg" href="<%: ResolveUrl("~/Account/Register.aspx?role=student") %>">Register as a student</a>
                    <a class="btn btn-light-outline btn-lg" href="<%: ResolveUrl("~/Account/Register.aspx?role=tutor") %>">Apply to tutor</a>
                </div>
            </div>
        </div>
    </section>
</asp:Content>
