namespace KFC_CRM.Entities.Box;

public class Box
{
    public long TelegramId { get; set; }
    public List<Meal.Meal> Meals { get; set; }
}
