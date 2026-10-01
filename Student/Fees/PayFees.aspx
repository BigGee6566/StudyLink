<%@ Page Title="Pay Fees" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="PayFees.aspx.cs" Inherits="StudyLink.Web.Student.Fees.PayFees" %>
<%@ Register TagPrefix="sl" TagName="CardForm" Src="~/UserControls/CardForm.ascx" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC15 - Pay Fees: choose the items, pay once by card --%>
    <asp:PlaceHolder ID="PayArea" runat="server">
        <div class="detail-grid">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Choose what to pay</h2></div>
                <div class="panel__body">
                    <div class="item-list">
                        <asp:Repeater ID="Fees" runat="server">
                            <ItemTemplate>
                                <label class="item" style="cursor:pointer">
                                    <input type="checkbox" class="form-check-input" name="fee" value="<%# Eval("FeeId") %>" <%# IsChosen((int)Eval("FeeId")) ? "checked=\"checked\"" : "" %> />
                                    <span class="item__main"><span class="item__title d-block"><%#: Eval("Description") %></span><span class="item__meta"><%#: Fmt.Words(Eval("Type")) %><%#: Eval("SourceReference") == null ? "" : " · " + Eval("SourceReference") %></span></span>
                                    <span class="item__side fw-semibold numeric"><%# Fmt.Money(Eval("Amount")) %></span>
                                </label>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <div class="summary-total"><span>Outstanding</span><span class="numeric"><asp:Literal ID="Outstanding" runat="server" /></span></div>
                </div>
            </div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Card details</h2></div>
                <div class="panel__body">
                    <sl:CardForm ID="Card" runat="server" />
                    <asp:Button ID="PayButton" runat="server" CssClass="btn btn-accent btn-lg btn-block mt-2" Text="Pay selected fees" OnClick="Pay_Click" data-once="Processing payment…" />
                </div>
            </div>
        </div>
    </asp:PlaceHolder>
    <asp:Literal ID="NothingDue" runat="server" />
</asp:Content>