<%@ Page Title="System Settings" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Settings.aspx.cs" Inherits="StudyLink.Web.Admin.Settings" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="panel">
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Setting</th><th>Value</th><th>Last changed</th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Setting"><strong><%#: Label((string)Eval("SettingKey")) %></strong><div class="muted small"><%#: Eval("Description") %></div></td>
                                <td data-label="Value" style="min-width:220px">
                                    <asp:HiddenField ID="Key" runat="server" Value='<%# Eval("SettingKey") %>' />
                                    <asp:HiddenField ID="Original" runat="server" Value='<%# Eval("Value") %>' />
                                    <asp:TextBox ID="Value" runat="server" CssClass="form-control form-control-sm" Text='<%# Eval("Value") %>' MaxLength="400" />
                                </td>
                                <td data-label="Last changed" class="small"><%# Fmt.DateTime((DateTime)Eval("UpdatedAt")) %></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <div class="panel__footer d-flex justify-content-between align-items-center flex-wrap gap-2">
            <span class="muted small">Each change is validated and recorded in the audit log with its old and new value.</span>
            <asp:Button ID="SaveButton" runat="server" CssClass="btn btn-primary" Text="Save changes" OnClick="Save_Click" />
        </div>
    </div>
</asp:Content>
