using KFC_CRM.Entities.Commons;

namespace KFC_CRM.Entities.Customer;

public class Customer : Auditable
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Phone { get; set; }
    public long TelegramId { get; set; }
}
