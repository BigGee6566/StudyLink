<%@ Page Title="My Subjects" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Subjects.aspx.cs" Inherits="StudyLink.Web.Tutor.Subjects" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="detail-grid">
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title">Subjects you tutor</h2></div>
            <div class="panel__body">
                <div class="item-list">
                    <asp:Repeater ID="List" runat="server" OnItemCommand="List_Command">
                        <ItemTemplate>
                            <div class="item">
                                <div class="item__main"><div class="item__title"><%#: Eval("Subject") %></div><div class="item__meta"><%#: Eval("StudyLevel") %><%# (bool)Eval("SubjectActive") ? "" : " · no longer offered" %></div></div>
                                <div class="item__side">
                                    <asp:LinkButton runat="server" CssClass="btn btn-sm btn-link btn-link-danger" CommandName="remove" CommandArgument='<%# Eval("Id") %>'
                                        Visible='<%# !(bool)Eval("InUse") %>' data-confirm="Remove this subject?">Remove</asp:LinkButton>
                                    <%# (bool)Eval("InUse") ? "<span class=\"muted small\">Has bookings</span>" : "" %>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
                <asp:Literal ID="EmptyState" runat="server" />
            </div>
        </div>
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title">Add a subject</h2></div>
            <div class="panel__body">
                <div class="mb-2"><label class="form-label" for="<%= Subject.ClientID %>">Subject</label><asp:DropDownList ID="Subject" runat="server" CssClass="form-select" /></div>
                <div class="mb-3"><label class="form-label" for="<%= Level.ClientID %>">Level</label><asp:DropDownList ID="Level" runat="server" CssClass="form-select" /></div>
                <asp:Button ID="AddButton" runat="server" CssClass="btn btn-primary btn-block" Text="Add subject" OnClick="Add_Click" />
            </div>
        </div>
    </div>
</asp:Content>