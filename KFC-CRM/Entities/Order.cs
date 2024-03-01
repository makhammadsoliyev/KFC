namespace KFC.Entities;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int Number { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime Date { get; set; }
}
