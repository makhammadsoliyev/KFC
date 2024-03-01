using KFC_CRM.Entities.Commons;

namespace KFC_CRM.Entities.Category;

public class Category : Auditable
{
    public int Id { get; set; }
    public string Name { get; set; }
}
