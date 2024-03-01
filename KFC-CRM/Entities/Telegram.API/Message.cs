using Newtonsoft.Json;

namespace KFC_CRM.Entities.Telegram.API;

public class Message
{
    [JsonProperty("message_id")]
    public int MessageId { get; set; }
    [JsonProperty("from")]
    public User From { get; set; }
    [JsonProperty("chat")]
    public Chat Chat { get; set; }
    [JsonProperty("date")]
    public int Date { get; set; }
    [JsonProperty("text")]
    public string Text { get; set; }
}
