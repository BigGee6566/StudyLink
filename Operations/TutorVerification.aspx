<%@ Page Title="Tutor Verification" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="TutorVerification.aspx.cs" Inherits="StudyLink.Web.Operations.TutorVerification" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC22 - Tutor Verification --%>
    <asp:Literal ID="TabStrip" runat="server" />
    <div class="detail-grid">
        <div class="panel">
            <div class="table-wrap">
                <table class="data-table stack">
                    <thead><tr><th>Tutor</th><th>Campus</th><th>Qualification</th><th>Subjects</th><th>Submitted</th><th>Status</th><th class="col-action"></th></tr></thead>
                    <tbody>
                        <asp:Repeater ID="Queue" runat="server">
                            <ItemTemplate>
                                <tr>
                                    <td data-label="Tutor"><%#: Eval("FullName") %><div class="muted small"><%#: Eval("Email") %></div></td>
                                    <td data-label="Campus"><%#: Eval("Campus") %></td>
                                    <td data-label="Qualification"><%#: Eval("Qualification") %><div class="muted small"><%# Eval("ExperienceYears") %> yrs · <%# Fmt.Money(Eval("HourlyRate")) %>/h</div></td>
                                    <td data-label="Subjects" class="small"><%#: Eval("Subjects") %></td>
                                    <td data-label="Submitted"><%# Fmt.Date((DateTime)Eval("SubmittedAt")) %></td>
                                    <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %></td>
                                    <td class="col-action"><a class="btn btn-sm btn-outline-primary" href="<%# OpenUrl((int)Eval("TutorId")) %>">Review</a></td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
            <asp:Literal ID="EmptyState" runat="server" />
        </div>

        <div>
            <asp:PlaceHolder ID="AppPanel" runat="server" Visible="false">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title"><asp:Literal ID="AppName" runat="server" /></h2><asp:Literal ID="AppStatus" runat="server" /></div>
                    <div class="panel__body">
                        <dl class="fact-list mb-3">
                            <dt>Email</dt><dd><asp:Literal ID="AppEmail" runat="server" /></dd>
                            <dt>Cell</dt><dd><asp:Literal ID="AppCell" runat="server" /></dd>
                            <dt>Campus</dt><dd><asp:Literal ID="AppCampus" runat="server" /></dd>
                            <dt>Qualification</dt><dd><asp:Literal ID="AppQualification" runat="server" /></dd>
                            <dt>Experience</dt><dd><asp:Literal ID="AppExperience" runat="server" /></dd>
                            <dt>Rate</dt><dd><asp:Literal ID="AppRate" runat="server" /></dd>
                            <dt>Subjects</dt><dd><asp:Literal ID="AppSubjects" runat="server" /></dd>
                        </dl>
                        <p class="small"><asp:Literal ID="AppBio" runat="server" /></p>

                        <asp:PlaceHolder ID="DecideForm" runat="server">
                            <h3 class="section-title">Checks</h3>
                            <div class="form-check"><asp:CheckBox ID="IdentityChecked" runat="server" /><label class="form-check-label" for="<%= IdentityChecked.ClientID %>">Identity document checked</label></div>
                            <div class="form-check mb-2"><asp:CheckBox ID="QualificationChecked" runat="server" /><label class="form-check-label" for="<%= QualificationChecked.ClientID %>">Qualification certificate checked</label></div>
                        </asp:PlaceHolder>
                        <label class="form-label" for="<%= Notes.ClientID %>">Notes / reason</label>
                        <asp:TextBox ID="Notes" runat="server" CssClass="form-control mb-3" TextMode="MultiLine" Rows="2" MaxLength="500" />
                        <div class="d-flex gap-2 flex-wrap">
                            <asp:Button ID="ApproveButton" runat="server" CssClass="btn btn-primary" Text="Approve tutor" OnClick="Approve_Click" />
                            <asp:Button ID="RejectButton" runat="server" CssClass="btn btn-outline-danger" Text="Reject" OnClick="Reject_Click" data-confirm="Reject this application?" />
                            <asp:Button ID="SuspendButton" runat="server" CssClass="btn btn-outline-danger" Text="Suspend tutor" OnClick="Suspend_Click" data-confirm="Suspend this tutor? Students will no longer be able to book them." />
                            <asp:Button ID="ReinstateButton" runat="server" CssClass="btn btn-primary" Text="Reinstate tutor" OnClick="Reinstate_Click" />
                        </div>
                    </div>
                </div>
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title">History</h2></div>
                    <div class="panel__body">
                        <div class="item-list">
                            <asp:Repeater ID="History" runat="server">
                                <ItemTemplate>
                                    <div class="item"><div class="item__main"><div class="item__title"><%# Fmt.Badge(Eval("Decision")) %> <%#: Eval("ReviewedBy") %></div>
                                        <div class="item__meta">Submitted <%# Fmt.Date((DateTime)Eval("SubmittedAt")) %><%# Eval("ReviewedAt") == null ? "" : " · reviewed " + Fmt.Date((DateTime?)Eval("ReviewedAt")) %> · ID <%# (bool)Eval("IdentityChecked") ? "✓" : "✗" %> · Qualification <%# (bool)Eval("QualificationChecked") ? "✓" : "✗" %></div>
                                        <div class="small"><%#: Eval("Notes") %></div></div></div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </div>
                </div>
            </asp:PlaceHolder>
            <asp:Literal ID="Hint" runat="server" />
        </div>
    </div>
</asp:Content>
