using KFC_CRM.Entities.Commons;

namespace KFC_CRM.Entities.Payment;


public class Payment : Auditable
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string PaymentMethod { get; set; }
}