

using KFC_CRM.Constants;
using KFC_CRM.Services;
using Telegram.Bot;

var client = new TelegramBotClient(CONSTANTS.BOTTOKEN);

TelegramService telegramService = new TelegramService(client);

await telegramService.Run();
