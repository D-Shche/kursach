using System;
using System.Data.SQLite;
using System.Linq;
using Dapper;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CryptoPriceBot.Services
{
    public static class HistoryService
    {
        private static readonly string connectionString = "Data Source=crypto_bot.db;Version=3;";

        public static void AddToHistory(long chatId, string query)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            connection.Execute("INSERT INTO QueryHistory (ChatId, QueryText, DateTime) VALUES (@ChatId, @QueryText, @DateTime)",
                new { ChatId = chatId, QueryText = query, DateTime = DateTime.Now });

            Console.WriteLine($"[History] Додано до історії: {query}");
        }

        public static async Task ShowHistory(ITelegramBotClient bot, Message message)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            var history = connection.Query("SELECT QueryText, DateTime FROM QueryHistory WHERE ChatId = @ChatId ORDER BY DateTime DESC LIMIT 5", new { ChatId = message.Chat.Id });

            if (!history.AsList().Any())
            {
                await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Історії запитів ще немає.");
                return;
            }

            string result = "Останні 5 запитів:\n" + string.Join("\n", history.Select(h => $"{h.QueryText} ({h.DateTime})"));
            await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: result);
            Console.WriteLine($"[History] Відправлено історію запитів користувачу {message.Chat.Id}");
        }
    }
}
