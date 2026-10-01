<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CardForm.ascx.cs" Inherits="StudyLink.Web.UserControls.CardForm" %>
<%-- Card entry shared by SC9 Payment and SC15 Pay Fees. Only the brand and last four digits are ever stored (D-6). --%>
<div class="card-form">
    <div class="card-preview" aria-hidden="true">
        <div class="small text-uppercase opacity-75">Card payment</div>
        <div class="fs-5 fw-bold mt-3 numeric">•••• •••• •••• ••••</div>
        <div class="d-flex justify-content-between small mt-2"><span>Card holder</span><span>MM/YY</span></div>
    </div>
    <div class="mb-3">
        <label class="form-label" for="<%= Holder.ClientID %>">Name on card</label>
        <asp:TextBox ID="Holder" runat="server" CssClass="form-control" MaxLength="100" autocomplete="cc-name" />
    </div>
    <div class="mb-3">
        <label class="form-label" for="<%= Number.ClientID %>">Card number</label>
        <asp:TextBox ID="Number" runat="server" CssClass="form-control numeric" MaxLength="23" autocomplete="cc-number" inputmode="numeric" data-card-number="true" placeholder="0000 0000 0000 0000" />
    </div>
    <div class="row g-3 mb-2">
        <div class="col-6">
            <label class="form-label" for="<%= Expiry.ClientID %>">Expiry</label>
            <asp:TextBox ID="Expiry" runat="server" CssClass="form-control numeric" MaxLength="5" autocomplete="cc-exp" inputmode="numeric" data-card-expiry="true" placeholder="MM/YY" />
        </div>
        <div class="col-6">
            <label class="form-label" for="<%= Cvv.ClientID %>">CVV</label>
            <asp:TextBox ID="Cvv" runat="server" CssClass="form-control numeric" MaxLength="4" TextMode="Password" autocomplete="cc-csc" inputmode="numeric" />
        </div>
    </div>
    <p class="secure-note"><%= Icons.Svg("lock", 14) %> Your card details are sent straight to the payment gateway. StudyLink keeps only the card type and last four digits.</p>
</div>
