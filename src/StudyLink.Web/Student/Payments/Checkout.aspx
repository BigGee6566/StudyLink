<%@ Page Title="Payment" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="Checkout.aspx.cs" Inherits="StudyLink.Web.Student.Payments.Checkout" %>
<%@ Register TagPrefix="sl" TagName="CardForm" Src="~/UserControls/CardForm.ascx" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <%-- SC9 - Payment: one card payment settles the reserved item(s) --%>
    <asp:PlaceHolder ID="CheckoutArea" runat="server">
        <div class="detail-grid">
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Card details</h2></div>
                <div class="panel__body">
                    <sl:CardForm ID="Card" runat="server" />
                    <asp:Button ID="PayButton" runat="server" CssClass="btn btn-accent btn-lg btn-block mt-2" OnClick="Pay_Click" data-once="Processing payment…" />
                    <p class="form-hint text-center mt-2"><a href="<%: ResolveUrl("~/Student/Fees/MyFees.aspx") %>">Pay later from My Fees</a> (reservations are released if unpaid when the hold ends).</p>
                </div>
            </div>
            <div class="panel">
                <div class="panel__header"><h2 class="panel__title">Order summary</h2></div>
                <div class="panel__body">
                    <asp:Literal ID="HoldNote" runat="server" />
                    <div class="summary-box">
                        <asp:Repeater ID="Lines" runat="server">
                            <ItemTemplate>
                                <div class="d-flex justify-content-between gap-3 mb-2">
                                    <span><%#: Eval("Description") %><br /><span class="muted small"><%#: Fmt.Words(Eval("Type")) %><%#: Eval("SourceReference") == null ? "" : " · " + Eval("SourceReference") %></span></span>
                                    <span class="fw-semibold numeric text-nowrap"><%# Fmt.Money(Eval("Amount")) %></span>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                        <div class="summary-total"><span>Total</span><span class="numeric"><asp:Literal ID="Total" runat="server" /></span></div>
                    </div>
                </div>
            </div>
        </div>
    </asp:PlaceHolder>
    <asp:PlaceHolder ID="NothingArea" runat="server" Visible="false">
        <div class="panel"><div class="panel__body"><asp:Literal ID="NothingMessage" runat="server" /></div></div>
    </asp:PlaceHolder>
</asp:Content>