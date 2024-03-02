using KFC_CRM.Constants;
using KFC_CRM.Entities.Customer;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace KFC_CRM.Services;

public class TelegramService
{
    private TelegramBotClient botClient;
    public TelegramService(TelegramBotClient telegramBotClient)
    {
        this.botClient = telegramBotClient;
    }
    public async Task Run()
    {


        botClient.StartReceiving(Update, Error);
        Console.ReadLine();
    }
    public async Task Update(ITelegramBotClient client, Update update, CancellationToken token)
    {
        var message = update.Message;

        if (message.Contact != null || message.Location != null)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
            {
                new KeyboardButton[] { "Orders", "Meals", "Categories" },
            })
            {
                ResizeKeyboard = true
            };

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Now choose:",
                replyMarkup: replyKeyboardMarkup
            );

            // Create a new user object with the relevant information
            var user = new Customer
            {
                Id = message.From.Id,
                FirstName = message.From.FirstName,
                LastName = message.From.LastName,
                Phone = message.Contact?.PhoneNumber,
                TelegramId = message.Chat.Id
            };

            // Serialize the user object to JSON
            string userJson = JsonConvert.SerializeObject(user, Formatting.Indented);

            // Write the user JSON to the users.json file
            string filePath = CONSTANTS.USERSPATH;
            await System.IO.File.AppendAllTextAsync(filePath, userJson + Environment.NewLine);
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Share your contact",
                replyMarkup: new ReplyKeyboardMarkup(new[]
                {
                    new[] { KeyboardButton.WithRequestContact("Share Contact") },
                })
            );
        }

        if (message != null)
        {
            if (message.Text == "Orders")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Entered Orders");
            }
            else if (message.Text == "Meals")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Entered Meals");
            }
            else if (message.Text == "Categories")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Entered Categories");
            }
        }
    }

    public List<Customer> GetCustomersAsync()
    {
        string jsonContent = System.IO.File.ReadAllText(CONSTANTS.USERSPATH);
        return JsonConvert.DeserializeObject<List<Customer>>(jsonContent);
    }

    public async static Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}