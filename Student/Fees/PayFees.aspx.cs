using System;
using System.Collections.Generic;
using System.Linq;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Student.Fees
{
    /// <summary>SC15 - settle several fees with one card payment (allocations sum to the payment exactly).</summary>
    [RequirePermission(Permission.PayFees)]
    public partial class PayFees : BasePage
    {
        private HashSet<int> _chosen = new HashSet<int>();

        private Guid Token
        {
            get => ViewState["token"] is Guid g ? g : Guid.Empty;
            set => ViewState["token"] = value;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Select the items to settle and pay by card.";
            if (IsPostBack) return;
            Token = Guid.NewGuid();
            Bind(QueryInt("fee") is int only ? new[] { only } : null);
        }

        private void Bind(IEnumerable<int> chosen)
        {
            var st = S.Fees.Statement(Actor);
            _chosen = new HashSet<int>(chosen ?? st.Unpaid.Select(f => f.FeeId));
            Fees.DataSource = st.Unpaid;
            Fees.DataBind();
            Outstanding.Text = Fmt.Money(st.Outstanding);
            PayArea.Visible = st.Unpaid.Count > 0;
            NothingDue.Text = st.Unpaid.Count == 0
                ? "<div class=\"panel\"><div class=\"panel__body\">" + Ui.Empty("check", "You have no outstanding fees.",
                  "<a class=\"btn btn-primary\" href=\"" + ResolveUrl("~/Student/Fees/MyFees.aspx") + "\">Back to My Fees</a>") + "</div></div>"
                : "";
        }

        protected bool IsChosen(int feeId) => _chosen.Contains(feeId);

        protected void Pay_Click(object sender, EventArgs e)
        {
            var ids = (Request.Form.GetValues("fee") ?? new string[0]).Select(Ui.Int).Where(i => i.HasValue).Select(i => i.Value).ToList();
            var result = S.Payments.Pay(Actor, ids, Card.Card, Token);
            Card.Clear();
            if (result.Succeeded)
            {
                Go("~/Student/Fees/PaymentComplete.aspx?id=" + result.Value);
                return;
            }
            Fail(result.Error);
            Token = Guid.NewGuid();
            Bind(ids);
        }
    }
}