<%@ Page Title="Subjects" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Subjects.aspx.cs" Inherits="StudyLink.Web.Operations.Subjects" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="detail-grid">
        <div class="panel">
            <div class="table-wrap">
                <table class="data-table stack">
                    <thead><tr><th>Subject</th><th class="col-num">Tutors</th><th class="col-num">Students</th><th>Status</th><th class="col-action"></th></tr></thead>
                    <tbody>
                        <asp:Repeater ID="Rows" runat="server" OnItemCommand="Row_Command">
                            <ItemTemplate>
                                <tr>
                                    <td data-label="Subject"><%#: Eval("Name") %></td>
                                    <td data-label="Tutors" class="col-num"><%# Eval("Tutors") %></td>
                                    <td data-label="Students" class="col-num"><%# Eval("Students") %></td>
                                    <td data-label="Status"><%# Fmt.Badge((bool)Eval("IsActive") ? "Active" : "Inactive") %></td>
                                    <td class="col-action">
                                        <a class="btn btn-sm btn-outline-primary" href="<%# ResolveUrl("~/Operations/Subjects.aspx?edit=" + Eval("SubjectId")) %>">Rename</a>
                                        <asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-secondary" CommandName="toggle" CommandArgument='<%# Eval("SubjectId") + ":" + Eval("IsActive") %>'><%# (bool)Eval("IsActive") ? "Retire" : "Restore" %></asp:LinkButton>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
        </div>
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title"><asp:Literal ID="FormTitle" runat="server" Text="Add a subject" /></h2></div>
            <div class="panel__body">
                <label class="form-label" for="<%= SubjectName.ClientID %>">Name</label>
                <asp:TextBox ID="SubjectName" runat="server" CssClass="form-control mb-3" MaxLength="80" />
                <asp:Button ID="SaveButton" runat="server" CssClass="btn btn-primary btn-block" Text="Save" OnClick="Save_Click" />
                <p class="form-hint mt-2">Retired subjects stay on past bookings but can no longer be chosen for new availability.</p>
            </div>
        </div>
    </div>
</asp:Content>
