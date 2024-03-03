using KFC_CRM.Entities.Commons;

namespace KFC_CRM.Entities.Order;

public class Order : Auditable
{
    public long CustomerId { get; set; }
    public int Number { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
}