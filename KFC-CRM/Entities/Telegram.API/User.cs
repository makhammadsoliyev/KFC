using Newtonsoft.Json;
using System.Numerics;

namespace KFC_CRM.Entities.Telegram.API;

public class User
{
    [JsonProperty("id")]
    public long Id { get; set; }
    [JsonProperty("is_bot")]
    public bool IsBot { get; set; }
    [JsonProperty("first_name")]
    public string FirstName { get; set; }
    [JsonProperty("last_name")]
    public string LastName { get; set; }
    [JsonProperty("username")]
    public string Username { get; set; }
}
