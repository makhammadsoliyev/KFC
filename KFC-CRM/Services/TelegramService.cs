using KFC_CRM.Constants;
using KFC_CRM.Entities.Box;
using KFC_CRM.Entities.Customer;
using KFC_CRM.Entities.Meal;
using KFC_CRM.Entities.Telegram.API;
using Newtonsoft.Json;
using System;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
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

        await Console.Out.WriteLineAsync($"{message?.From?.FirstName}  |  {message?.Text}");

        if (update.CallbackQuery != null)
        {
            var boxes = GetBoxes() ?? new List<Box>();

            if (update.CallbackQuery.Data.Contains("__delete"))
            {
                var mealName = update.CallbackQuery.Data.Replace("__delete", "").Trim();
                var box = boxes.FirstOrDefault(b => b.TelegramId == update.CallbackQuery.From.Id);

                if (box != null)
                {
                    var mealsToRemove = box.Meals.Where(m => m.Name == mealName).ToList();

                    if (mealsToRemove.Count > 0)
                    {
                        foreach (var mealToRemove in mealsToRemove)
                        {
                            box.Meals.Remove(mealToRemove);
                        }

                        await SaveBoxesAsync(boxes);
                        await client.SendTextMessageAsync(update.CallbackQuery.From.Id, $"All meals with the name '{mealName}' have been removed from the box.");
                    }
                    else
                    {
                        await client.SendTextMessageAsync(update.CallbackQuery.From.Id, $"No meals found with the name '{mealName}' in the box.");
                    }
                }
            }
            else if (!update.CallbackQuery.Data.Contains("__minus"))
            {
                var meal = GetMeal(update.CallbackQuery.Data);
                var box = boxes.FirstOrDefault(b => b.TelegramId == update.CallbackQuery.From.Id);

                if (box is null)
                {
                    box = new Box()
                    {
                        TelegramId = update.CallbackQuery.From.Id,
                        Meals = new List<Meal>()
                    };
                    box.Meals.Add(meal);
                    boxes.Add(box);
                }

                box.Meals.Add(meal);
                await SaveBoxesAsync(boxes);
                await client.SendTextMessageAsync(update.CallbackQuery.From.Id, "Korzinka qo'shildi");
            }
            else
            {
                var mealName = update.CallbackQuery.Data.Replace("__minus", "").Trim();
                var box = boxes.FirstOrDefault(b => b.TelegramId == update.CallbackQuery.From.Id);

                if (box != null)
                {
                    var mealToRemove = box.Meals.FirstOrDefault(m => m.Name == mealName);

                    if (mealToRemove != null)
                    {
                        box.Meals.Remove(mealToRemove);
                        await SaveBoxesAsync(boxes);
                        await client.SendTextMessageAsync(update.CallbackQuery.From.Id, "Meal removed from the box.");
                    }
                }
            }
        }
        else if (GetCustomers().Any(c => c.Id == message?.From?.Id))
        {
            var users = GetCustomers();
            if (users.FirstOrDefault(c => c.Id == message?.From?.Id).Phone is not null)
            {
                ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
                {
                    new KeyboardButton[] { "Orders", "Meals", "My Box" },
                })
                {
                    ResizeKeyboard = true
                };

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    "Now choose:",
                    replyMarkup: replyKeyboardMarkup
                );
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
                await Console.Out.WriteLineAsync($" /// {message.Contact?.PhoneNumber} ///");

                var user = new Customer
                {
                    Id = message.From.Id,
                    FirstName = message.From.FirstName,
                    LastName = message.From.LastName,
                    Phone = message.Contact?.PhoneNumber,
                    TelegramId = message.Chat.Id
                };

                users = GetCustomers();
                var existingUser = users.FirstOrDefault(u => u.Id == user.Id);

                if (existingUser != null)
                {
                    existingUser.Phone = user.Phone;

                    // Serialize the user object to JSON
                    string userJson = JsonConvert.SerializeObject(users, Formatting.Indented);

                    // Write the user JSON to the users.json file
                    string filePath = CONSTANTS.USERSPATH;
                    await System.IO.File.WriteAllTextAsync(filePath, userJson);
                }
            }
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

            var user = new Customer
            {
                Id = message.From.Id,
                FirstName = message.From.FirstName,
                LastName = message.From.LastName,
                Phone = message.Contact?.PhoneNumber,
                TelegramId = message.Chat.Id
            };
            var users = GetCustomers();
            users.Add( user );

            // Serialize the user object to JSON
            string userJson = JsonConvert.SerializeObject(users, Formatting.Indented);

            // Write the user JSON to the users.json file
            string filePath = CONSTANTS.USERSPATH;
            await System.IO.File.WriteAllTextAsync(filePath, userJson);
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

                var meals = GetMeals();
                var buttons = new List<KeyboardButton[]>();

                foreach (var meal in meals)
                {
                    var button = new KeyboardButton(meal.Name);
                    buttons.Add(new KeyboardButton[] { button });
                }

                var replyKeyboardMarkup = new ReplyKeyboardMarkup(buttons);
                await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Please select a meal:",
                    replyMarkup: replyKeyboardMarkup
                );
            }

            else if (message.Text == "My Box")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Entered My Box");

                var boxes = GetBoxes();
                if (boxes.DefaultIfEmpty() != null)
                {
                    var box = boxes.FirstOrDefault(b => b.TelegramId == message.From.Id);
                    if (box is null)
                    {
                        await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "No meals found! You should "
                            );
                    }
                    else if (box.Meals.Count() != 0)
                    {
                        var inlineKeyboardButtons = new List<InlineKeyboardButton[]>();

                        foreach (var meal in box.Meals.DistinctBy(m => m.Name))
                        {
                            var plusButton = InlineKeyboardButton.WithCallbackData($"+", $"{meal.Name}");
                            var minusButton = InlineKeyboardButton.WithCallbackData($"-", $"{meal.Name}__minus");
                            var removeProductButton = InlineKeyboardButton.WithCallbackData($" X {meal.Name} X ", $"{meal.Name}__delete");
                            var quantityButton = InlineKeyboardButton.WithCallbackData($"{box.Meals.Count(m => m.Name == meal.Name)}");
                            inlineKeyboardButtons.Add(new[] { removeProductButton });
                            inlineKeyboardButtons.Add(new[] { minusButton, quantityButton, plusButton });
                        }

                        var inlineKeyboardMarkup = new InlineKeyboardMarkup(inlineKeyboardButtons);

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "Here are the meals in your box:",
                            replyMarkup: inlineKeyboardMarkup
                        );
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "No meals found!"
                            );
                    }
                }
                
            }

            if (GetMeals().Any(m => m.Name == message.Text))
            {
                var meals = GetMeals();
                var meal = meals.FirstOrDefault(m => m.Name == message.Text);

                if (meal != null)
                {
                    using (var webClient = new WebClient())
                    {
                        byte[] photoBytes = webClient.DownloadData(meal.PictureUrl);
                        using (var memoryStream = new MemoryStream(photoBytes))
                        {
                            var photo = new InputFileStream(memoryStream, "photo.jpg");
                            await botClient.SendPhotoAsync(
                                chatId: message.Chat.Id,
                                photo: photo,
                                caption: message.Text
                            );
                        }
                    }

                    var inlineKeyboardMarkup = new InlineKeyboardMarkup(new[]
                    {
                        new []
                        {
                            InlineKeyboardButton.WithCallbackData("Add to box", $"{meal.Name}")
                        }
                    });

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"Meal: {meal.Name}\nDescription: {meal.Description}\nPrice: {meal.Price}",
                        replyMarkup:  inlineKeyboardMarkup
                    );

                }
            }
        }
    }

    private async Task SaveBoxesAsync(List<Box> boxes)
    {
        string boxJson = JsonConvert.SerializeObject(boxes, Formatting.Indented);

        // Write the user JSON to the users.json file
        string filePath = CONSTANTS.BOXPATH;
        await System.IO.File.WriteAllTextAsync(filePath, boxJson);
    }

    public List<Customer> GetCustomers()
    {
        string jsonContent = System.IO.File.ReadAllText(CONSTANTS.USERSPATH);
        return JsonConvert.DeserializeObject<List<Customer>>(jsonContent);
    }

    public List<Meal> GetMeals()
    {
        string jsonContent = System.IO.File.ReadAllText(CONSTANTS.MEALSPATH);
        return JsonConvert.DeserializeObject<List<Meal>>(jsonContent);
    }

    public List<Box> GetBoxes()
    {
        string jsonContent = System.IO.File.ReadAllText(CONSTANTS.BOXPATH);
        return JsonConvert.DeserializeObject<List<Box>>(jsonContent);
    }

    public Meal GetMeal(string name)
    {
        var meals = GetMeals();
        return meals.FirstOrDefault(m => m.Name == name);
    }

    public async static Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}