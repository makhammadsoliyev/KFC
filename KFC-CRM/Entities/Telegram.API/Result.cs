using Newtonsoft.Json;

namespace KFC_CRM.Entities.Telegram.API;

public class Result
{
    [JsonProperty("update_id")]
    public long UpdateId { get; set; }
    [JsonProperty("message")]
    public Message Message { get; set; }
}
