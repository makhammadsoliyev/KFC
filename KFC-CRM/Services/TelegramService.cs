using KFC_CRM.Constants;
using Newtonsoft.Json;
using Spectre.Console;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KFC_CRM.Services;

public class TelegramService
{
    private readonly TelegramBotClient _telegramBotClient;
    public TelegramService(TelegramBotClient telegramBotClient)
    {
        this._telegramBotClient = telegramBotClient;
    }
    public async Task Run()
    {
        _telegramBotClient.StartReceiving(Update, Error);
        Console.ReadLine();
    }
    public async static Task Update(ITelegramBotClient client, Update update, CancellationToken token)
    {
        var message = update.Message;
        if (message != null)
        {
            if (message.Text.ToLower().Contains("hi"))
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Assalomu alaykum");
            }
        }
    }
    public async static Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}