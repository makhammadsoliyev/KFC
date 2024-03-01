using KFC_CRM.Entities.Telegram.API;

public class Message
{
    public int Message_Id { get; set; }
    public User From { get; set; }
    public Chat Chat { get; set; }
    public int Date { get; set; }
    public string Text { get; set; }
    public Audio Audio { get; set; }
}
