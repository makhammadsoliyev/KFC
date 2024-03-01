
using Newtonsoft.Json;

namespace KFC_CRM.Entities.Telegram.API;

public class TelegramResponse
{
    [JsonProperty("ok")]
    public bool Ok { get; set; }
    [JsonProperty("result")]
    public List<Result> Result { get; set; }
}
