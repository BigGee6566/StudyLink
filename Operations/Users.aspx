<%@ Page Title="User Management" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Users.aspx.cs" Inherits="StudyLink.Web.Operations.Users" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC23 - User Management (scope follows the signed-in role: Admin all, Manager staff/suppliers/students, Staff students) --%>
    <asp:MultiView ID="Views" runat="server" ActiveViewIndex="0">
        <asp:View ID="ListView" runat="server">
            <div class="panel">
                <div class="panel__body">
                    <asp:Panel runat="server" CssClass="filter-bar" DefaultButton="SearchButton">
                        <div><label class="form-label" for="<%= SearchText.ClientID %>">Search</label><asp:TextBox ID="SearchText" runat="server" CssClass="form-control" MaxLength="100" placeholder="Name, username, email or number" /></div>
                        <div><label class="form-label" for="<%= RoleFilter.ClientID %>">Role</label><asp:DropDownList ID="RoleFilter" runat="server" CssClass="form-select" /></div>
                        <div><label class="form-label" for="<%= ActiveFilter.ClientID %>">Status</label>
                            <asp:DropDownList ID="ActiveFilter" runat="server" CssClass="form-select"><asp:ListItem Value="">All</asp:ListItem><asp:ListItem Value="1">Active</asp:ListItem><asp:ListItem Value="0">Inactive</asp:ListItem></asp:DropDownList></div>
                        <div class="filter-actions d-flex gap-2">
                            <asp:Button ID="SearchButton" runat="server" CssClass="btn btn-primary" Text="Search" OnClick="Search_Click" />
                            <a class="btn btn-accent" id="NewLink" runat="server">+ New account</a>
                        </div>
                    </asp:Panel>
                </div>
            </div>
            <div class="panel">
                <div class="table-wrap">
                    <table class="data-table stack">
                        <thead><tr><th>Name</th><th>Username</th><th>Role</th><th>Campus</th><th>Last sign-in</th><th>Status</th><th class="col-action"></th></tr></thead>
                        <tbody>
                            <asp:Repeater ID="Rows" runat="server">
                                <ItemTemplate>
                                    <tr>
                                        <td data-label="Name"><%#: Eval("FullName") %><div class="muted small"><%#: Eval("Email") %><%#: string.IsNullOrEmpty((string)Eval("Extra")) ? "" : " · " + Eval("Extra") %></div></td>
                                        <td data-label="Username"><%#: Eval("UserName") %></td>
                                        <td data-label="Role"><%#: Eval("Roles") %></td>
                                        <td data-label="Campus"><%#: Eval("Campus") %></td>
                                        <td data-label="Last sign-in"><%# Fmt.DateTime((DateTime?)Eval("LastLoginAt")) %></td>
                                        <td data-label="Status"><%# Fmt.Badge((bool)Eval("IsActive") ? "Active" : "Inactive") %><%# (bool)Eval("IsLocked") ? " " + Fmt.Badge("Locked") : "" %></td>
                                        <td class="col-action"><a class="btn btn-sm btn-outline-primary" href="<%# ResolveUrl("~/Operations/Users.aspx?id=" + Eval("UserId")) %>">Manage</a></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
                <asp:Literal ID="EmptyState" runat="server" />
                <asp:Literal ID="Pager" runat="server" />
            </div>
        </asp:View>

        <asp:View ID="EditView" runat="server">
            <p class="mb-3"><a href="<%: ResolveUrl("~/Operations/Users.aspx") %>">&larr; All users</a></p>
            <div class="detail-grid">
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title"><asp:Literal ID="FormTitle" runat="server" /></h2></div>
                    <div class="panel__body">
                        <asp:PlaceHolder ID="RoleChoice" runat="server">
                            <div class="mb-3" style="max-width:320px"><label class="form-label" for="<%= NewRole.ClientID %>">Account type</label>
                                <asp:DropDownList ID="NewRole" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="NewRole_Changed" /></div>
                        </asp:PlaceHolder>
                        <div class="row g-3">
                            <div class="col-md-6"><label class="form-label" for="<%= FirstName.ClientID %>">First name</label><asp:TextBox ID="FirstName" runat="server" CssClass="form-control" MaxLength="50" /></div>
                            <div class="col-md-6"><label class="form-label" for="<%= LastName.ClientID %>">Last name</label><asp:TextBox ID="LastName" runat="server" CssClass="form-control" MaxLength="50" /></div>
                            <div class="col-md-6"><label class="form-label" for="<%= UserName.ClientID %>">Username</label><asp:TextBox ID="UserName" runat="server" CssClass="form-control" MaxLength="30" /></div>
                            <div class="col-md-6"><label class="form-label" for="<%= Email.ClientID %>">Email</label><asp:TextBox ID="Email" runat="server" CssClass="form-control" TextMode="Email" MaxLength="150" /></div>
                            <div class="col-md-6"><label class="form-label" for="<%= Cell.ClientID %>">Cell number</label><asp:TextBox ID="Cell" runat="server" CssClass="form-control" MaxLength="15" /></div>
                            <div class="col-md-3"><label class="form-label" for="<%= Campus.ClientID %>">Campus</label><asp:DropDownList ID="Campus" runat="server" CssClass="form-select" /></div>
                            <div class="col-md-3"><label class="form-label" for="<%= Gender.ClientID %>">Gender</label><asp:DropDownList ID="Gender" runat="server" CssClass="form-select" /></div>
                            <asp:PlaceHolder ID="EmployeeFields" runat="server">
                                <div class="col-md-6"><label class="form-label" for="<%= EmployeeNumber.ClientID %>">Employee number</label><asp:TextBox ID="EmployeeNumber" runat="server" CssClass="form-control" MaxLength="20" /></div>
                                <div class="col-md-6"><label class="form-label" for="<%= JobTitle.ClientID %>">Job title</label><asp:TextBox ID="JobTitle" runat="server" CssClass="form-control" MaxLength="80" /></div>
                            </asp:PlaceHolder>
                            <asp:PlaceHolder ID="SupplierFields" runat="server">
                                <div class="col-md-6"><label class="form-label" for="<%= CompanyName.ClientID %>">Company name</label><asp:TextBox ID="CompanyName" runat="server" CssClass="form-control" MaxLength="120" /></div>
                                <div class="col-md-6"><label class="form-label" for="<%= ContactPerson.ClientID %>">Contact person</label><asp:TextBox ID="ContactPerson" runat="server" CssClass="form-control" MaxLength="100" /></div>
                                <div class="col-md-4"><label class="form-label" for="<%= VatNumber.ClientID %>">VAT number</label><asp:TextBox ID="VatNumber" runat="server" CssClass="form-control" MaxLength="20" /></div>
                                <div class="col-md-4"><label class="form-label" for="<%= Phone.ClientID %>">Office phone</label><asp:TextBox ID="Phone" runat="server" CssClass="form-control" MaxLength="20" /></div>
                                <div class="col-12"><label class="form-label" for="<%= Address.ClientID %>">Address</label><asp:TextBox ID="Address" runat="server" CssClass="form-control" MaxLength="250" /></div>
                            </asp:PlaceHolder>
                            <asp:PlaceHolder ID="StudentFields" runat="server">
                                <div class="col-md-4"><label class="form-label" for="<%= StudentNumber.ClientID %>">Student number</label><asp:TextBox ID="StudentNumber" runat="server" CssClass="form-control" MaxLength="10" placeholder="S202600001" autocomplete="off" /><div class="form-hint">S followed by 9 digits.</div></div>
                                <div class="col-md-4"><label class="form-label" for="<%= StudyLevel.ClientID %>">Study level</label><asp:DropDownList ID="StudyLevel" runat="server" CssClass="form-select" /></div>
                                <div class="col-md-4"><label class="form-label" for="<%= Faculty.ClientID %>">Faculty</label><asp:TextBox ID="Faculty" runat="server" CssClass="form-control" MaxLength="100" /></div>
                            </asp:PlaceHolder>
                            <asp:PlaceHolder ID="TempPasswordField" runat="server">
                                <div class="col-md-6"><label class="form-label" for="<%= TemporaryPassword.ClientID %>">Temporary password</label>
                                    <asp:TextBox ID="TemporaryPassword" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" autocomplete="new-password" data-password-toggle="true" />
                                    <div class="form-hint">At least 8 characters with a letter and a number. <button type="button" class="btn btn-link btn-sm p-0 align-baseline" data-generate-password="<%= TemporaryPassword.ClientID %>">Generate one</button></div></div>
                            </asp:PlaceHolder>
                        </div>
                        <div class="form-actions">
                            <asp:Button ID="CreateButton" runat="server" CssClass="btn btn-primary" Text="Create account" OnClick="Create_Click" />
                            <asp:Button ID="SaveButton" runat="server" CssClass="btn btn-primary" Text="Save details" OnClick="Save_Click" />
                        </div>
                        <p class="form-hint" id="CreateHint" runat="server">The new user is emailed their username and this temporary password, and must choose their own password the first time they sign in.</p>
                    </div>
                </div>

                <asp:PlaceHolder ID="AccountSide" runat="server">
                    <div>
                        <div class="panel">
                            <div class="panel__header"><h2 class="panel__title">Account</h2><asp:Literal ID="StatusBadge" runat="server" /></div>
                            <div class="panel__body">
                                <dl class="fact-list mb-3">
                                    <dt>Created</dt><dd><asp:Literal ID="Created" runat="server" /></dd>
                                    <dt>Last sign-in</dt><dd><asp:Literal ID="LastLogin" runat="server" /></dd>
                                    <dt>Failed sign-ins</dt><dd><asp:Literal ID="Failed" runat="server" /></dd>
                                </dl>
                                <label class="form-label" for="<%= Reason.ClientID %>">Reason (recorded in the audit log)</label>
                                <asp:TextBox ID="Reason" runat="server" CssClass="form-control mb-2" MaxLength="300" />
                                <div class="d-flex gap-2 flex-wrap">
                                    <asp:Button ID="DeactivateButton" runat="server" CssClass="btn btn-outline-danger" Text="Deactivate" OnClick="Deactivate_Click" data-confirm="Deactivate this account? The user is signed out immediately." />
                                    <asp:Button ID="ActivateButton" runat="server" CssClass="btn btn-primary" Text="Reactivate" OnClick="Activate_Click" />
                                    <asp:Button ID="UnlockButton" runat="server" CssClass="btn btn-outline-primary" Text="Unlock" OnClick="Unlock_Click" />
                                </div>
                            </div>
                        </div>
                        <asp:PlaceHolder ID="TempPasswordPanel" runat="server">
                            <div class="panel">
                                <div class="panel__header"><h2 class="panel__title">Temporary password</h2></div>
                                <asp:Panel runat="server" CssClass="panel__body" DefaultButton="SetTempPasswordButton">
                                    <p class="form-hint mt-0 mb-3">Replaces the user's password and emails them the new one. They must choose their own password when they next sign in.</p>
                                    <label class="form-label" for="<%= NewTempPassword.ClientID %>">New temporary password</label>
                                    <asp:TextBox ID="NewTempPassword" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" autocomplete="new-password" data-password-toggle="true" />
                                    <div class="form-hint mb-2">At least 8 characters with a letter and a number. <button type="button" class="btn btn-link btn-sm p-0 align-baseline" data-generate-password="<%= NewTempPassword.ClientID %>">Generate one</button></div>
                                    <asp:Button ID="SetTempPasswordButton" runat="server" CssClass="btn btn-outline-primary" Text="Set and email password" OnClick="SetTempPassword_Click" data-confirm="Replace this user's password? They will be emailed the new one." />
                                </asp:Panel>
                            </div>
                        </asp:PlaceHolder>
                        <asp:PlaceHolder ID="RolesPanel" runat="server">
                            <div class="panel">
                                <div class="panel__header"><h2 class="panel__title">Roles</h2></div>
                                <div class="panel__body">
                                    <div class="item-list mb-3">
                                        <asp:Repeater ID="RoleList" runat="server" OnItemCommand="Role_Command">
                                            <ItemTemplate>
                                                <div class="item"><div class="item__main"><div class="item__title"><%#: Fmt.Words(Container.DataItem) %></div></div>
                                                    <div class="item__side"><asp:LinkButton runat="server" CssClass="btn btn-sm btn-link btn-link-danger" CommandName="remove" CommandArgument='<%# Container.DataItem %>' data-confirm="Remove this role?">Remove</asp:LinkButton></div></div>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </div>
                                    <div class="filter-bar">
                                        <asp:DropDownList ID="AddRole" runat="server" CssClass="form-select" aria-label="Role to add" />
                                        <asp:Button ID="AddRoleButton" runat="server" CssClass="btn btn-outline-primary" Text="Assign role" OnClick="AddRole_Click" />
                                    </div>
                                </div>
                            </div>
                        </asp:PlaceHolder>
                    </div>
                </asp:PlaceHolder>
            </div>
        </asp:View>
    </asp:MultiView>
</asp:Content>
