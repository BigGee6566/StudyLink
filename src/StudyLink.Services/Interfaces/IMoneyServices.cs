using System;
using System.Collections.Generic;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;

namespace StudyLink.Services.Interfaces
{
    public interface IFeeService
    {
        /// <summary>SC14: the signed-in student's balance, unpaid items and history.</summary>
        FeeStatement Statement(Actor actor);

        /// <summary>SC9/SC15: validates that each fee belongs to the student and is still payable.</summary>
        ServiceResult<CheckoutSummary> PrepareCheckout(Actor actor, IList<int> feeIds);

        /// <summary>Staff view of one student's statement (assisting at the counter).</summary>
        FeeStatement StatementFor(Actor actor, int studentId);
    }

    public interface IPaymentService
    {
        /// <summary>
        /// Charges the card for the selected fees and settles them in one transaction. The checkout
        /// token makes a double submit return the first payment instead of charging twice.
        /// </summary>
        ServiceResult<int> Pay(Actor actor, IList<int> feeIds, CardDetails card, Guid checkoutToken);

        Receipt Receipt(Actor actor, int paymentId);
        PagedResult<PaymentRow> Search(Actor actor, PaymentFilter filter);

        /// <summary>Manager-initiated refund of a paid fee.</summary>
        ServiceResult Refund(Actor actor, int feeId, string reason);

        /// <summary>
        /// Refund required by a business rule (tutor declines or cancels a paid session). Called by
        /// other services on the acting user's behalf; not exposed on any page.
        /// </summary>
        ServiceResult RefundForRule(Actor actor, int feeId, string reason);
    }
}
