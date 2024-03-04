using KFC_CRM.Constants;
using KFC_CRM.Entities.Box;
using KFC_CRM.Entities.Customer;
using KFC_CRM.Entities.Meal;
using KFC_CRM.Entities.Order;
using Newtonsoft.Json;
using Npgsql;
using Spectre.Console;
using System.Net;
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

        AnsiConsole.MarkupLine($"[yellow]{message?.From?.FirstName}[/]  |  {message?.Text}");

        if (update.CallbackQuery != null)
        {
            var boxes = GetBoxes() ?? new List<Box>();

            if (update.CallbackQuery.Data == "Order")
            {
                decimal totalPrice = 0;

                var meal = GetMeal(update.CallbackQuery.Data);

                var box = boxes.FirstOrDefault(b => b.TelegramId == update.CallbackQuery.From.Id);

                var distinctMeals = box.Meals.DistinctBy(m => m.Name);

                foreach (var meall in distinctMeals)
                {
                    var quantity = box.Meals.Count(m => m.Name == meall.Name);
                    var price = meall.Price * quantity;
                    totalPrice += price;
                }
                var order = new Order
                {
                    CustomerId = update.CallbackQuery.From.Id,
                    TotalAmount = totalPrice
                };

                // Generate a unique order number
                var orderNumber = GenerateOrderNumber();
                order.Number = orderNumber;

                // Save the order to the orders.json file
                var orders = LoadOrders();
                orders.Add(order);
                await botClient.SendTextMessageAsync(update.CallbackQuery.From.Id, "Your order has been placed successfully!");

                // Reset the box for the customer
                box.Meals.Clear();
                await SaveBoxesAsync(boxes);
                SaveOrders(orders, order);
            }
            else if (update.CallbackQuery.Data.Contains("__delete"))
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
                await client.SendTextMessageAsync(update.CallbackQuery.From.Id, "Added to the box");
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
                    await SaveUsersAsync(users);
                }
            }
        }
        else if (update.MyChatMember is not null)
        {
            Console.WriteLine("Something went wrong!");
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
            users.Add(user);

            await SaveUsersAsync(users);
        }

        if (message != null)
        {
            if (message.Text == "Orders")
            {
                // Load the orders for the customer
                var orders = LoadOrdersForCustomer(message.Chat.Id);

                if (orders.Count > 0)
                {
                    // Format and send the orders to the customer
                    var ordersText = FormatOrdersText(orders);
                    await botClient.SendTextMessageAsync(message.Chat.Id, ordersText);
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "You have no orders yet.");
                }
            }
            else if (message.Text == "Meals")
            {
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
                        decimal totalPrice = 0;

                        await botClient.SendTextMessageAsync(message.Chat.Id, "📥 Box:");

                        var distinctMeals = box.Meals.DistinctBy(m => m.Name);

                        foreach (var meal in distinctMeals)
                        {
                            var quantity = box.Meals.Count(m => m.Name == meal.Name);
                            var price = meal.Price * quantity;
                            totalPrice += price;

                            var mealText = $"{quantity}. {meal.Name}\n{quantity} x {meal.Price} = {price} $";

                            await botClient.SendTextMessageAsync(message.Chat.Id, mealText);
                            var orderButton = InlineKeyboardButton.WithCallbackData("Order");
                            var plusButton = InlineKeyboardButton.WithCallbackData($"+", $"{meal.Name}");
                            var minusButton = InlineKeyboardButton.WithCallbackData($"-", $"{meal.Name}__minus");
                            var removeProductButton = InlineKeyboardButton.WithCallbackData($" X {meal.Name} X ", $"{meal.Name}__delete");
                            var quantityButton = InlineKeyboardButton.WithCallbackData($"{quantity}");
                            inlineKeyboardButtons.Add(new[] { removeProductButton });
                            inlineKeyboardButtons.Add(new[] { minusButton, quantityButton, plusButton });
                            inlineKeyboardButtons.Add(new[] { orderButton });
                        }

                        var totalPriceText = $"\n\nOverall: {totalPrice} $";
                        await botClient.SendTextMessageAsync(message.Chat.Id, totalPriceText);

                        var inlineKeyboardMarkup = new InlineKeyboardMarkup(inlineKeyboardButtons);

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "To place your order, click the button below:",
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
                        replyMarkup: inlineKeyboardMarkup
                    );

                }
            }
        }
    }

    // Format the orders into a readable text format
    private string FormatOrdersText(List<Order> orders)
    {
        var ordersText = "Your orders:\n\n";

        foreach (var order in orders)
        {
            ordersText += $"Order Number: {order.Number}\n";
            ordersText += $"Total Amount: {order.TotalAmount} $\n";
            ordersText += $"Date: {order.Date.ToString("yyyy-MM-dd HH:mm:ss")}\n\n";
        }

        return ordersText;
    }
    // Generate a unique order number
    private int GenerateOrderNumber()
    {
        Random random = new Random();
        return random.Next(100, 10000);
    }

    // Load existing orders from the orders.json file
    private List<Order> LoadOrders()
    {
        // Read the contents of the orders.json file
        string ordersJson = System.IO.File.ReadAllText(CONSTANTS.ORDERPATH);

        // Deserialize the JSON string to a list of Order objects
        List<Order> orders = JsonConvert.DeserializeObject<List<Order>>(ordersJson);

        // If the orders.json file doesn't exist or is empty, return an empty list
        if (orders == null)
        {
            orders = new List<Order>();
        }

        return orders;
    }
    // Load orders specifically for the customer
    private List<Order> LoadOrdersForCustomer(long customerId)
    {
        var orders = LoadOrders();
        return orders.Where(o => o.CustomerId == customerId).ToList();
    }

    // Save the orders to the orders.json file
    private void SaveOrders(List<Order> orders, Order order)
    {
        // Serialize the list of Order objects to a JSON string
        string ordersJson = JsonConvert.SerializeObject(orders, Formatting.Indented);

        // Write the JSON string to the orders.json file
        System.IO.File.WriteAllText(CONSTANTS.ORDERPATH, ordersJson);

        using (var connection = new NpgsqlConnection(CONSTANTS.DB_CONNECTION_STRING))
        {
            connection.Open();

            // Create a command to insert order into the database

            string insertQuery = "INSERT INTO orders (customer_id, number, total_amount, date) " +
                                    "VALUES (@CustomerId, @Number, @TotalAmount, @Date)";

            using (var command = new NpgsqlCommand(insertQuery, connection))
            {
                // Set the parameter values
                command.Parameters.AddWithValue("@CustomerId", order.CustomerId);
                command.Parameters.AddWithValue("@Number", order.Number);
                command.Parameters.AddWithValue("@TotalAmount", order.TotalAmount);
                command.Parameters.AddWithValue("@Date", order.Date);

                // Execute the insert command
                command.ExecuteNonQuery();
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

    private async Task SaveUsersAsync(List<Customer> users)
    {
        string userJson = JsonConvert.SerializeObject(users, Formatting.Indented);

        // Write the user JSON to the users.json file
        string filePath = CONSTANTS.USERSPATH;
        await System.IO.File.WriteAllTextAsync(filePath, userJson);
    }

    public async static Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}