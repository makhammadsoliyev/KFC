using KFC_CRM.Entities.Commons;

namespace KFC_CRM.Entities.Order;

public class OrderItem : Auditable
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int MealId { get; set; }
    public int Quantity { get; set; }
}
