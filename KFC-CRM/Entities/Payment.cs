namespace KFC.Entities;


public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string PaymentMethod { get; set; }
}