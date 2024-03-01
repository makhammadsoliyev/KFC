using KFC_CRM.Entities.Commons;

namespace KFC_CRM.Entities.Customer;

public class Customer : Auditable
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Phone { get; set; }
    public int TelegramId { get; set; }
}
