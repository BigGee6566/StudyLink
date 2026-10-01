<%@ Page Title="Reports" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Reports.aspx.cs" Inherits="StudyLink.Web.Operations.Reports" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC24 - Reports Dashboard: every figure is computed from live data --%>
    <div class="panel">
        <div class="panel__body">
            <div class="filter-bar">
                <div style="flex:2 1 260px"><label class="form-label" for="<%= Report.ClientID %>">Report</label><asp:DropDownList ID="Report" runat="server" CssClass="form-select" /></div>
                <div><label class="form-label" for="<%= From.ClientID %>">From</label><asp:TextBox ID="From" runat="server" CssClass="form-control" TextMode="Date" /></div>
                <div><label class="form-label" for="<%= To.ClientID %>">To</label><asp:TextBox ID="To" runat="server" CssClass="form-control" TextMode="Date" /></div>
                <asp:Button ID="RunButton" runat="server" CssClass="btn btn-primary" Text="Run report" OnClick="Run_Click" />
            </div>
            <p class="form-hint mb-0"><asp:Literal ID="Description" runat="server" /></p>
        </div>
    </div>
    <asp:PlaceHolder ID="ResultPanel" runat="server" Visible="false">
        <asp:Literal ID="Summary" runat="server" />
        <div class="panel">
            <div class="panel__header">
                <h2 class="panel__title"><asp:Literal ID="ResultTitle" runat="server" /></h2>
                <div class="d-flex gap-2 no-print">
                    <a class="btn btn-sm btn-outline-primary" id="CsvLink" runat="server">Download CSV</a>
                    <button type="button" class="btn btn-sm btn-outline-primary" onclick="window.print()">Print</button>
                </div>
            </div>
            <div class="table-wrap"><asp:Literal ID="Table" runat="server" /></div>
            <div class="panel__footer muted small"><asp:Literal ID="Generated" runat="server" /></div>
        </div>
    </asp:PlaceHolder>
    <asp:PlaceHolder ID="Catalogue" runat="server">
        <div class="action-list" style="grid-template-columns:repeat(auto-fit,minmax(280px,1fr))">
            <asp:Repeater ID="Definitions" runat="server">
                <ItemTemplate>
                    <a class="action-card" href="<%# ResolveUrl("~/Operations/Reports.aspx?key=" + Eval("Key")) %>">
                        <span class="action-card__icon"><%# Icons.Svg("chart") %></span>
                        <span><span class="action-card__title d-block"><%#: Eval("Title") %></span><span class="action-card__text"><%#: Eval("Description") %></span></span>
                    </a>
                </ItemTemplate>
            </asp:Repeater>
        </div>
    </asp:PlaceHolder>
</asp:Content>
