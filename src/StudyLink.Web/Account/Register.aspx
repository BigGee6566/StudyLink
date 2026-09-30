<%@ Page Title="Create your account" Language="C#" MasterPageFile="~/MasterPages/Auth.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="StudyLink.Web.Account.Register" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="stepper" aria-hidden="true"><asp:Literal ID="Stepper" runat="server" /></div>
    <asp:MultiView ID="Steps" runat="server" ActiveViewIndex="0">

        <%-- SC3 - Role Selection --%>
        <asp:View ID="RoleStep" runat="server">
            <h1>Join StudyLink</h1>
            <p class="lead">Who are you registering as? Staff, supplier and manager accounts are created by the hub.</p>
            <div class="role-choice">
                <label>
                    <asp:RadioButton ID="RoleStudent" runat="server" GroupName="role" Checked="true" />
                    <span><strong>Student</strong>Rent or buy textbooks, book tutors and pay your fees online.</span>
                </label>
                <label>
                    <asp:RadioButton ID="RoleTutor" runat="server" GroupName="role" />
                    <span><strong>Tutor</strong>Offer tutoring sessions. Hub staff verify your identity and qualification before students can book you.</span>
                </label>
            </div>
            <asp:Button ID="RoleNext" runat="server" Text="Continue" CssClass="btn btn-primary btn-lg btn-block mt-4" OnClick="RoleNext_Click" />
            <p class="text-center mt-3">Already registered? <a href="<%: ResolveUrl("~/Account/Login.aspx") %>">Log in</a></p>
        </asp:View>

        <%-- SC2 - Register Page (account details) --%>
        <asp:View ID="AccountStep" runat="server">
            <h1><asp:Literal ID="AccountHeading" runat="server" /></h1>
            <p class="lead">Your account details. Fields marked * are required.</p>
            <div class="row g-3">
                <div class="col-sm-6"><label class="form-label" for="<%= FirstName.ClientID %>">First name *</label><asp:TextBox ID="FirstName" runat="server" CssClass="form-control" MaxLength="50" autocomplete="given-name" /></div>
                <div class="col-sm-6"><label class="form-label" for="<%= LastName.ClientID %>">Last name *</label><asp:TextBox ID="LastName" runat="server" CssClass="form-control" MaxLength="50" autocomplete="family-name" /></div>
                <div class="col-12"><label class="form-label" for="<%= Email.ClientID %>">Email address *</label><asp:TextBox ID="Email" runat="server" CssClass="form-control" TextMode="Email" MaxLength="100" autocomplete="email" /></div>
                <div class="col-sm-6"><label class="form-label" for="<%= Cell.ClientID %>">Cell number *</label><asp:TextBox ID="Cell" runat="server" CssClass="form-control" MaxLength="15" placeholder="082 123 4567" autocomplete="tel" /></div>
                <div class="col-sm-6"><label class="form-label" for="<%= Campus.ClientID %>">Campus *</label><asp:DropDownList ID="Campus" runat="server" CssClass="form-select" DataTextField="Name" DataValueField="Id" /></div>
                <div class="col-sm-6"><label class="form-label" for="<%= UserName.ClientID %>">Username *</label><asp:TextBox ID="UserName" runat="server" CssClass="form-control" MaxLength="50" autocomplete="username" /><div class="form-hint">3–50 letters, numbers, dot, dash or underscore.</div></div>
                <div class="col-sm-6"><label class="form-label" for="<%= Gender.ClientID %>">Gender</label>
                    <asp:DropDownList ID="Gender" runat="server" CssClass="form-select">
                        <asp:ListItem Value="PreferNotToSay" Text="Prefer not to say" />
                        <asp:ListItem Value="Female" Text="Female" />
                        <asp:ListItem Value="Male" Text="Male" />
                        <asp:ListItem Value="Other" Text="Other" />
                    </asp:DropDownList></div>
                <div class="col-sm-6"><label class="form-label" for="<%= Password.ClientID %>">Password *</label><asp:TextBox ID="Password" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" autocomplete="new-password" /><div class="form-hint">At least 8 characters, with a letter and a number.</div></div>
                <div class="col-sm-6"><label class="form-label" for="<%= ConfirmPassword.ClientID %>">Confirm password *</label><asp:TextBox ID="ConfirmPassword" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" autocomplete="new-password" /></div>
            </div>
            <div class="form-actions">
                <asp:Button ID="AccountBack" runat="server" Text="Back" CssClass="btn btn-outline-secondary" OnClick="Back_Click" CausesValidation="false" />
                <asp:Button ID="AccountNext" runat="server" Text="Continue" CssClass="btn btn-primary flex-fill" OnClick="AccountNext_Click" />
            </div>
        </asp:View>

        <%-- Student details --%>
        <asp:View ID="StudentStep" runat="server">
            <h1>Your studies</h1>
            <p class="lead">Your student number links your rentals, bookings and fees.</p>
            <div class="mb-3"><label class="form-label" for="<%= StudentNumber.ClientID %>">Student number *</label><asp:TextBox ID="StudentNumber" runat="server" CssClass="form-control" MaxLength="20" /></div>
            <div class="mb-3"><label class="form-label" for="<%= StudyLevel.ClientID %>">Year of study</label><asp:DropDownList ID="StudyLevel" runat="server" CssClass="form-select" DataTextField="Name" DataValueField="Id" AppendDataBoundItems="true"><asp:ListItem Value="" Text="Select…" /></asp:DropDownList></div>
            <div class="mb-3"><label class="form-label" for="<%= Faculty.ClientID %>">Faculty</label><asp:TextBox ID="Faculty" runat="server" CssClass="form-control" MaxLength="100" placeholder="e.g. Management & Commerce" /></div>
            <div class="form-check mb-3">
                <asp:CheckBox ID="StudentTerms" runat="server" />
                <label for="<%= StudentTerms.ClientID %>">I agree to the terms of use and consent to StudyLink processing my personal information as described in the <a href="<%: ResolveUrl("~/Public/Privacy.aspx") %>" target="_blank">POPIA privacy notice</a>.</label>
            </div>
            <div class="form-actions">
                <asp:Button ID="StudentBack" runat="server" Text="Back" CssClass="btn btn-outline-secondary" OnClick="Back_Click" CausesValidation="false" />
                <asp:Button ID="StudentCreate" runat="server" Text="Create account" CssClass="btn btn-primary flex-fill" OnClick="Create_Click" data-once="Creating account…" />
            </div>
        </asp:View>

        <%-- Tutor details --%>
        <asp:View ID="TutorStep" runat="server">
            <h1>Your tutoring profile</h1>
            <p class="lead">Hub staff check this against your ID and certified qualification before you can be booked.</p>
            <div class="row g-3">
                <div class="col-12"><label class="form-label" for="<%= Qualification.ClientID %>">Highest qualification *</label><asp:TextBox ID="Qualification" runat="server" CssClass="form-control" MaxLength="100" placeholder="e.g. BCom Accounting (Hons)" /></div>
                <div class="col-sm-6"><label class="form-label" for="<%= Experience.ClientID %>">Years of tutoring experience *</label><asp:TextBox ID="Experience" runat="server" CssClass="form-control" TextMode="Number" min="0" max="70" /></div>
                <div class="col-sm-6"><label class="form-label" for="<%= HourlyRate.ClientID %>">Hourly rate (R) *</label><asp:TextBox ID="HourlyRate" runat="server" CssClass="form-control" TextMode="Number" min="0" step="5" /></div>
                <div class="col-12"><label class="form-label" for="<%= Bio.ClientID %>">Short bio</label><asp:TextBox ID="Bio" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" data-max="1000" /></div>
                <div class="col-12">
                    <span class="form-label d-block">Subjects you can tutor *</span>
                    <div class="subject-picks">
                        <asp:Repeater ID="SubjectPicks" runat="server">
                            <ItemTemplate>
                                <div class="d-flex gap-2 align-items-center">
                                    <asp:HiddenField ID="SubjectId" runat="server" Value='<%# Eval("Id") %>' />
                                    <asp:CheckBox ID="Pick" runat="server" Text='<%#: Eval("Name") %>' CssClass="flex-fill" />
                                    <asp:DropDownList ID="Level" runat="server" CssClass="form-select form-select-sm" style="max-width:170px" />
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </div>
            <div class="form-check my-3">
                <asp:CheckBox ID="TutorTerms" runat="server" />
                <label for="<%= TutorTerms.ClientID %>">I agree to the terms of use and consent to StudyLink processing my personal information as described in the <a href="<%: ResolveUrl("~/Public/Privacy.aspx") %>" target="_blank">POPIA privacy notice</a>.</label>
            </div>
            <div class="form-actions">
                <asp:Button ID="TutorBack" runat="server" Text="Back" CssClass="btn btn-outline-secondary" OnClick="Back_Click" CausesValidation="false" />
                <asp:Button ID="TutorCreate" runat="server" Text="Submit for verification" CssClass="btn btn-primary flex-fill" OnClick="Create_Click" data-once="Submitting…" />
            </div>
        </asp:View>

        <%-- SC4 - Registration confirmation --%>
        <asp:View ID="DoneStep" runat="server">
            <div class="text-center">
                <div class="confirm-card__icon"><%= Icons.Svg("check", 38) %></div>
                <h1><asp:Literal ID="DoneHeading" runat="server" /></h1>
                <p class="lead"><asp:Literal ID="DoneText" runat="server" /></p>
                <a class="btn btn-primary btn-lg btn-block" href="<%: ResolveUrl(LoginUrl) %>">Continue to log in</a>
            </div>
        </asp:View>
    </asp:MultiView>
</asp:Content>