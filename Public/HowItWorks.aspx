<%@ Page Title="How It Works" Language="C#" AutoEventWireup="true" CodeBehind="HowItWorks.aspx.cs" Inherits="StudyLink.Web.Public.HowItWorks" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="panel"><div class="panel__body">
        <h2 class="h4 fw-bold">For students</h2>
        <div class="steps mb-4">
            <div class="step"><div class="step__num">1</div><strong>Register</strong><p class="muted mb-0">Create a student account with your student number and choose your campus: Alice, East London or Bisho.</p></div>
            <div class="step"><div class="step__num">2</div><strong>Book a tutor</strong><p class="muted mb-0">Search verified tutors by subject and level, pick an open slot and pay to reserve it. The tutor then confirms; if they decline you're refunded in full.</p></div>
            <div class="step"><div class="step__num">3</div><strong>Rent or buy books</strong><p class="muted mb-0">See live stock per campus. A copy is held while you pay; collect it at the hub counter. Rentals run for <asp:Literal ID="Period" runat="server" /> days and can be renewed.</p></div>
            <div class="step"><div class="step__num">4</div><strong>Pay &amp; track</strong><p class="muted mb-0">Every fee appears under My Fees. Pay one or several items by card and get an instant receipt by email.</p></div>
        </div>
        <h2 class="h4 fw-bold">For tutors</h2>
        <div class="steps mb-4">
            <div class="step"><div class="step__num">1</div><strong>Apply</strong><p class="muted mb-0">Register as a tutor with your qualification, subjects and hourly rate.</p></div>
            <div class="step"><div class="step__num">2</div><strong>Get verified</strong><p class="muted mb-0">Hub staff check your identity and qualifications. Until then you can sign in and complete your profile.</p></div>
            <div class="step"><div class="step__num">3</div><strong>Publish slots</strong><p class="muted mb-0">Publish the times you're available. Slots can never be double-booked.</p></div>
            <div class="step"><div class="step__num">4</div><strong>Teach &amp; get paid</strong><p class="muted mb-0">Confirm sessions, record attendance and submit your weekly timesheet. Earnings are shown after the hub's commission.</p></div>
        </div>
        <h2 class="h4 fw-bold">Good to know</h2>
        <ul class="mb-0">
            <li>Late returns cost <strong>a daily late fee per book</strong>, shown on each book's page before you rent it.</li>
            <li>Paid sessions cancelled at least <asp:Literal ID="Cutoff" runat="server" /> hours before the start are refunded.</li>
            <li>Unpaid reservations are released automatically after <asp:Literal ID="Hold" runat="server" /> minutes so others can book.</li>
            <li>StudyLink never stores your full card number or CVV.</li>
        </ul>
    </div></div>
    <p class="text-center"><a class="btn btn-primary btn-lg" href="<%: ResolveUrl("~/Account/Register.aspx") %>">Create your account</a></p>
</asp:Content>