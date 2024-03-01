namespace KFC.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int MealId { get; set; }
    public int Quantity { get; set; }
}
