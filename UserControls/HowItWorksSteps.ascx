<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="HowItWorksSteps.ascx.cs" Inherits="StudyLink.Web.UserControls.HowItWorksSteps" %>
<%-- Four steps on a line that draws in when scrolled into view (studylink.js adds .is-visible). --%>
<div class="flow" role="list" data-reveal>
    <span class="flow__line" aria-hidden="true"></span>
    <% for (var i = 0; i < Steps.Count; i++) { %>
    <div class="flow__step" role="listitem" style="--i:<%= i %>">
        <div class="flow__icon"><%= Icons.Svg(Steps[i].Icon, 26) %><span class="flow__num"><%= i + 1 %></span></div>
        <h3><%: Steps[i].Title %></h3>
        <p><%: Steps[i].Text %></p>
    </div>
    <% } %>
</div>
