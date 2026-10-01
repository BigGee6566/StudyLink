using System.Web.UI;
using StudyLink.Services.DTOs;

namespace StudyLink.Web.UserControls
{
    /// <summary>Card fields for SC9 / SC15. Values live only for the postback that submits them.</summary>
    public partial class CardForm : UserControl
    {
        public CardDetails Card => new CardDetails
        {
            Holder = Holder.Text,
            Number = Number.Text,
            Expiry = Expiry.Text,
            Cvv = Cvv.Text
        };

        /// <summary>Card fields are never echoed back after a failed attempt.</summary>
        public void Clear()
        {
            Number.Text = string.Empty;
            Cvv.Text = string.Empty;
        }
    }
}
