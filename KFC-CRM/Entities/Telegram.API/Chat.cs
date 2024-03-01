using Newtonsoft.Json;

public class Chat
{
    [JsonProperty("id")]
    public long Id { get; set; }
    [JsonProperty("first_name")]
    public string FirstName { get; set; }
    [JsonProperty("last_name")]
    public string LastName { get; set; }
    [JsonProperty("title")]
    public string Title { get; set; }
    [JsonProperty("username")]
    public string Username { get; set; }
    [JsonProperty("type")]
    public string Type { get; set; }
}