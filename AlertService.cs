using System.Data.SQLite;
using Dapper;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CryptoPriceBot.Services
{
    public static class AlertService
    {
        private static readonly string connectionString = "Data Source=crypto_bot.db;Version=3;";

        public static async Task AddAlert(ITelegramBotClient bot, Message message)
        {
            var parts = message.Text.Split(' ');
            if (parts.Length != 4 || !(parts[2] == "above" || parts[2] == "below") || !double.TryParse(parts[3], out double target))
            {
                await bot.SendTextMessageAsync(message.Chat.Id, "Формат: /alert <crypto_symbol> <above|below> <ціна>");
                Console.WriteLine("[AddAlert] ❌ Невірний формат команди");
                return;
            }

            string symbol = parts[1].ToLower();
            string direction = parts[2].ToLower();

            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            connection.Execute("INSERT INTO Alerts (ChatId, CryptoSymbol, Direction, TargetPrice) VALUES (@ChatId, @Symbol, @Direction, @TargetPrice)",
                new { ChatId = message.Chat.Id, Symbol = symbol, Direction = direction, TargetPrice = target });

            string confirmation = $"Підписка на сповіщення для {symbol.ToUpper()} встановлена: {direction} {target}";
            await bot.SendTextMessageAsync(message.Chat.Id, confirmation);
            Console.WriteLine($"[AddAlert] ✅ {confirmation}");
        }

        public static async Task ClearAlerts(ITelegramBotClient bot, Message message)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            int deleted = connection.Execute("DELETE FROM Alerts WHERE ChatId = @ChatId", new { ChatId = message.Chat.Id });

            await bot.SendTextMessageAsync(message.Chat.Id, $"Видалено {deleted} підписок на сповіщення.");
            Console.WriteLine($"[ClearAlerts] Видалено {deleted} сповіщень для чату {message.Chat.Id}");
        }
    }
}
