<%@ Page Title="Backups" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Backups.aspx.cs" Inherits="StudyLink.Web.Admin.Backups" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="panel">
        <div class="panel__body d-flex justify-content-between align-items-center flex-wrap gap-3">
            <div><strong>Full database backup</strong><div class="muted small"><asp:Literal ID="Target" runat="server" /></div></div>
            <asp:Button ID="RunButton" runat="server" CssClass="btn btn-primary" Text="Back up now" OnClick="Run_Click" data-once="Backing up…" />
        </div>
    </div>
    <div class="panel">
        <div class="table-wrap">
            <table class="data-table stack">
                <thead><tr><th>Started</th><th>Finished</th><th>By</th><th>File</th><th class="col-num">Size</th><th>Status</th></tr></thead>
                <tbody>
                    <asp:Repeater ID="Rows" runat="server">
                        <ItemTemplate>
                            <tr>
                                <td data-label="Started"><%# Fmt.DateTime((DateTime)Eval("StartedAt")) %></td>
                                <td data-label="Finished"><%# Fmt.DateTime((DateTime?)Eval("CompletedAt")) %></td>
                                <td data-label="By"><%#: Eval("RequestedBy") %></td>
                                <td data-label="File" class="small text-break"><%#: Eval("FilePath") %></td>
                                <td data-label="Size" class="col-num"><%# Ui.Bytes((long?)Eval("SizeBytes")) %></td>
                                <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %><div class="muted small"><%#: Eval("Message") %></div></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </tbody>
            </table>
        </div>
        <asp:Literal ID="EmptyState" runat="server" />
    </div>
</asp:Content>
