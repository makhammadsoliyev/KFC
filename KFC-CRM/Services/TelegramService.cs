using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KFC_CRM.Services;

public class TelegramService
{
    public static async Task SendMessageAsync(string apiUrl, string botToken)
    {
        try
        {
            // Get the chat ID and message text from the user
            var chatId = await GetChatIdAsync(botToken);
            var text = AnsiConsole.Ask<string>("[bold]Enter the message text:[/]");

            using (HttpClient client = new HttpClient())
            {
                var content = new MultipartFormDataContent
            {
                { new StringContent(chatId), "chat_id" },
                { new StringContent(text), "text" }
            };

                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                AnsiConsole.MarkupLine("[green]Message sent successfully![/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Failed to send message. Error: " + ex.Message + "[/]");
        }
    }

    public static async Task<string> GetChatIdAsync(string botToken)
    {
        try
        {
            var filePath = "../../../../ASR/Data/Chats.json";

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Read the existing JSON data from the file
                var existingJson = File.ReadAllText(filePath);

                // Deserialize the existing JSON data into a list of chats
                var existingChats = JsonConvert.DeserializeObject<List<Chat>>(existingJson);

                if (existingChats.Count == 0)
                {
                    throw new InvalidOperationException("[red]No chat IDs found.[/]");
                }

                if (existingChats.Count == 1)
                {
                    return existingChats[0].Id.ToString();
                }

                var selectionPrompt = new SelectionPrompt<string>()
                    .Title("[underline]Select a chat ID:[/]")
                    .HighlightStyle("cyan")
                    .PageSize(10)
                    .AddChoices(existingChats.Select(c => $"{c.Id} -> {c.FirstName ?? c.Title} {c.LastName ?? "(Group)"}").ToList());

                return AnsiConsole.Prompt(selectionPrompt);
            }
            else
            {
                throw new FileNotFoundException("[red]Chats.json file not found.[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine("[red]Failed to get chat IDs. Error: " + ex.Message + "[/]");
            throw;
        }
    }
}
