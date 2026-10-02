<%@ Page Title="Privacy" Language="C#" AutoEventWireup="true" CodeBehind="Privacy.aspx.cs" Inherits="StudyLink.Web.Public.Privacy" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="panel"><div class="panel__body">
        <h2 class="h5 fw-bold">What we collect</h2>
        <p>Your name, contact details, campus and student or staff number; your bookings, rentals, fees and payments; and, for tutors, qualifications and availability. We record sign-ins and important actions in an audit log to protect your account.</p>
        <h2 class="h5 fw-bold">Card payments</h2>
        <p>Card details go to the payment gateway. StudyLink stores only the card type and the last four digits as part of your receipt, never the full number or CVV.</p>
        <h2 class="h5 fw-bold">Who can see your information</h2>
        <p>Only the people who need it: your tutor sees your name and contact details for sessions you book; hub staff see what they need to verify tutors, issue and receive books and help with your account; managers see financial and stock reports.</p>
        <h2 class="h5 fw-bold">Your rights</h2>
        <p class="mb-0">In line with POPIA you may ask to see or correct your information. Use <a href="<%: ResolveUrl("~/Public/Contact.aspx") %>">Contact</a> or the change-request form on your profile.</p>
    </div></div>
</asp:Content>