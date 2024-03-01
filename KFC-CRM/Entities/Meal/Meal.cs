using KFC_CRM.Entities.Commons;

namespace KFC_CRM.Entities.Meal;

public class Meal : Auditable
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string PictureUrl { get; set; }
    public int CategoryId { get; set; }
}
