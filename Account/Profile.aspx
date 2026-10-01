<%@ Page Title="Profile" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Profile.aspx.cs" Inherits="StudyLink.Web.Account.Profile" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="detail-grid">
        <div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Contact details</h2></div>
                <div class="panel__body">
                    <div class="row g-3">
                        <div class="col-md-6"><label class="form-label" for="<%= Email.ClientID %>">Email</label><asp:TextBox ID="Email" runat="server" CssClass="form-control" TextMode="Email" MaxLength="150" /></div>
                        <div class="col-md-6"><label class="form-label" for="<%= Cell.ClientID %>">Cell number</label><asp:TextBox ID="Cell" runat="server" CssClass="form-control" MaxLength="15" placeholder="0821234567" /></div>
                        <div class="col-md-6"><label class="form-label" for="<%= Campus.ClientID %>">Campus</label><asp:DropDownList ID="Campus" runat="server" CssClass="form-select" /></div>
                        <div class="col-md-6"><label class="form-label" for="<%= Gender.ClientID %>">Gender</label><asp:DropDownList ID="Gender" runat="server" CssClass="form-select" /></div>
                    </div>
                    <div class="form-actions"><asp:Button ID="SaveContact" runat="server" CssClass="btn btn-primary" Text="Save changes" OnClick="SaveContact_Click" /></div>
                </div>
            </div>

            <asp:PlaceHolder ID="TutorPanel" runat="server">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title">Tutor profile</h2><asp:Literal ID="TutorStatus" runat="server" /></div>
                    <div class="panel__body">
                        <div class="row g-3">
                            <div class="col-md-6"><label class="form-label" for="<%= Qualification.ClientID %>">Highest qualification</label><asp:TextBox ID="Qualification" runat="server" CssClass="form-control" MaxLength="150" /></div>
                            <div class="col-md-3"><label class="form-label" for="<%= Experience.ClientID %>">Years' experience</label><asp:TextBox ID="Experience" runat="server" CssClass="form-control" TextMode="Number" /></div>
                            <div class="col-md-3"><label class="form-label" for="<%= Rate.ClientID %>">Rate per hour (R)</label><asp:TextBox ID="Rate" runat="server" CssClass="form-control" inputmode="decimal" /></div>
                            <div class="col-12"><label class="form-label" for="<%= Bio.ClientID %>">About you</label><asp:TextBox ID="Bio" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="4" MaxLength="1000" data-max="1000" /></div>
                        </div>
                        <div class="form-actions"><asp:Button ID="SaveTutor" runat="server" CssClass="btn btn-primary" Text="Save tutor profile" OnClick="SaveTutor_Click" />
                            <a class="btn btn-outline-primary" href="<%: ResolveUrl("~/Tutor/Subjects.aspx") %>">My subjects</a></div>
                    </div>
                </div>
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="StudentPanel" runat="server">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title">My subjects</h2></div>
                    <div class="panel__body">
                        <div class="chips mb-3">
                            <asp:Repeater ID="StudentSubjects" runat="server" OnItemCommand="StudentSubject_Command">
                                <ItemTemplate><span class="chip"><%#: Eval("Subject") %> · <%#: Eval("StudyLevel") %>
                                    <asp:LinkButton runat="server" CommandName="remove" CommandArgument='<%# Eval("Id") %>' CssClass="ms-1 text-decoration-none" aria-label="Remove">&times;</asp:LinkButton></span></ItemTemplate>
                            </asp:Repeater>
                        </div>
                        <div class="filter-bar">
                            <asp:DropDownList ID="AddSubject" runat="server" CssClass="form-select" aria-label="Subject" />
                            <asp:DropDownList ID="AddLevel" runat="server" CssClass="form-select" aria-label="Level" />
                            <asp:Button ID="AddSubjectButton" runat="server" CssClass="btn btn-outline-primary" Text="Add" OnClick="AddSubject_Click" />
                        </div>
                    </div>
                </div>
            </asp:PlaceHolder>
        </div>

        <div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Account</h2></div>
                <div class="panel__body">
                    <dl class="fact-list mb-3">
                        <dt>Name</dt><dd><asp:Literal ID="FullName" runat="server" /></dd>
                        <dt>Username</dt><dd><asp:Literal ID="UserName" runat="server" /></dd>
                        <dt>Role</dt><dd><asp:Literal ID="Roles" runat="server" /></dd>
                        <asp:Literal ID="RoleFacts" runat="server" />
                        <dt>Member since</dt><dd><asp:Literal ID="Since" runat="server" /></dd>
                    </dl>
                    <a class="btn btn-outline-primary btn-block" href="<%: ResolveUrl("~/Account/ChangePassword.aspx") %>">Change password</a>
                </div>
            </div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Request a change</h2></div>
                <div class="panel__body">
                    <p class="muted small">Your name and identifying numbers are verified by the hub, so changes go to staff for approval.</p>
                    <div class="mb-2"><label class="form-label" for="<%= Field.ClientID %>">Field</label><asp:DropDownList ID="Field" runat="server" CssClass="form-select" /></div>
                    <div class="mb-2"><label class="form-label" for="<%= NewValue.ClientID %>">New value</label><asp:TextBox ID="NewValue" runat="server" CssClass="form-control" MaxLength="150" /></div>
                    <div class="mb-3"><label class="form-label" for="<%= Reason.ClientID %>">Reason</label><asp:TextBox ID="Reason" runat="server" CssClass="form-control" MaxLength="300" /></div>
                    <asp:Button ID="RequestButton" runat="server" CssClass="btn btn-outline-primary btn-block" Text="Send request" OnClick="Request_Click" />
                    <div class="item-list mt-3">
                        <asp:Repeater ID="Requests" runat="server">
                            <ItemTemplate>
                                <div class="item"><div class="item__main"><div class="item__title"><%#: Fmt.Words(Eval("FieldName")) %> → <%#: Eval("RequestedValue") %></div>
                                    <div class="item__meta"><%# Fmt.Date((DateTime)Eval("CreatedAt")) %><%#: Eval("ResponseNote") == null ? "" : " · " + Eval("ResponseNote") %></div></div>
                                    <div class="item__side"><%# Fmt.Badge(Eval("Status")) %></div></div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>