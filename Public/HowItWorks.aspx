<%@ Page Title="How It Works" Language="C#" AutoEventWireup="true" CodeBehind="HowItWorks.aspx.cs" Inherits="StudyLink.Web.Public.HowItWorks" %>
<%@ Register TagPrefix="sl" TagName="HowItWorksSteps" Src="~/UserControls/HowItWorksSteps.ascx" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <section class="mb-5">
        <div class="eyebrow">For students</div>
        <h2 class="h3 fw-bold mb-4" style="color:var(--navy-800)">Find help, get your books, pay in one place</h2>
        <sl:HowItWorksSteps ID="StudentSteps" runat="server" Audience="students" />
    </section>
    <section class="mb-5 pt-3">
        <div class="eyebrow">For tutors</div>
        <h2 class="h3 fw-bold mb-4" style="color:var(--navy-800)">Teach at your campus and get paid</h2>
        <sl:HowItWorksSteps ID="TutorSteps" runat="server" Audience="tutors" />
    </section>
    <div class="panel">
        <div class="panel__header"><h2 class="panel__title">Good to know</h2></div>
        <div class="panel__body">
            <ul class="checklist my-0">
                <li><%= Icons.Svg("check", 16) %><span>Late returns cost <strong>a daily late fee per book</strong>, shown on each book's page before you rent it.</span></li>
                <li><%= Icons.Svg("check", 16) %><span>Paid sessions cancelled at least <strong><asp:Literal ID="Cutoff" runat="server" /> hours</strong> before the start are refunded.</span></li>
                <li><%= Icons.Svg("check", 16) %><span>Unpaid reservations are released after <strong><asp:Literal ID="Hold" runat="server" /> minutes</strong> so others can book.</span></li>
                <li><%= Icons.Svg("check", 16) %><span>Rentals run for <strong><asp:Literal ID="Period" runat="server" /> days</strong> and can be renewed from My Rentals.</span></li>
                <li><%= Icons.Svg("check", 16) %><span>StudyLink never stores your full card number or CVV.</span></li>
            </ul>
        </div>
    </div>
    <p class="text-center mt-4"><a class="btn btn-primary btn-lg" href="<%: ResolveUrl("~/Account/Register.aspx") %>">Create your account</a></p>
</asp:Content>
