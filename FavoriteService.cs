using System.Data.SQLite;
using System.Linq;
using Dapper;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CryptoPriceBot.Services
{
    public static class FavoritesService
    {
        private static readonly string connectionString = "Data Source=crypto_bot.db;Version=3;";

        public static async Task AddFavorite(ITelegramBotClient bot, Message message)
        {
            var parts = message.Text.Split(' ');
            if (parts.Length < 3)
            {
                await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Використовуйте: /addfavorite <crypto_symbol> <date>");
                return;
            }

            string symbol = parts[1];
            string date = parts[2];

            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            var exists = connection.QueryFirstOrDefault("SELECT 1 FROM Favorites WHERE ChatId = @ChatId AND CryptoSymbol = @Symbol AND Date = @Date",
                new { ChatId = message.Chat.Id, Symbol = symbol, Date = date });

            if (exists != null)
            {
                await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Цей запит вже у списку улюблених.");
                return;
            }

            connection.Execute("INSERT INTO Favorites (ChatId, CryptoSymbol, Date) VALUES (@ChatId, @Symbol, @Date)",
                new { ChatId = message.Chat.Id, Symbol = symbol, Date = date });

            await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: $"{symbol.ToUpper()} на {date} додано в улюблені.");
            Console.WriteLine($"[Favorites] Додано: {symbol.ToUpper()} на {date} для чату {message.Chat.Id}");
        }

        public static async Task RemoveFavorite(ITelegramBotClient bot, Message message)
        {
            var parts = message.Text.Split(' ');
            if (parts.Length < 3)
            {
                await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Використовуйте: /removefavorite <crypto_symbol> <date>");
                return;
            }

            string symbol = parts[1];
            string date = parts[2];

            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            var exists = connection.QueryFirstOrDefault("SELECT 1 FROM Favorites WHERE ChatId = @ChatId AND CryptoSymbol = @Symbol AND Date = @Date",
                new { ChatId = message.Chat.Id, Symbol = symbol, Date = date });

            if (exists == null)
            {
                await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Цей запис не знайдено в улюблених.");
                return;
            }

            connection.Execute("DELETE FROM Favorites WHERE ChatId = @ChatId AND CryptoSymbol = @Symbol AND Date = @Date",
                new { ChatId = message.Chat.Id, Symbol = symbol, Date = date });

            await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: $"{symbol.ToUpper()} на {date} видалено з улюблених.");
            Console.WriteLine($"[Favorites] Видалено: {symbol.ToUpper()} на {date} для чату {message.Chat.Id}");
        }

        public static async Task ShowFavorites(ITelegramBotClient bot, Message message)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            var favorites = connection.Query("SELECT CryptoSymbol, Date FROM Favorites WHERE ChatId = @ChatId", new { ChatId = message.Chat.Id });

            if (!favorites.AsList().Any())
            {
                await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Улюблених запитів ще немає.");
                return;
            }

            string result = "Ваші улюблені запити:\n" + string.Join("\n", favorites.Select(f => $"{f.CryptoSymbol.ToUpper()} на {f.Date}"));
            await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: result);
            Console.WriteLine($"[Favorites] Показано список улюблених для чату {message.Chat.Id}");
        }
    }
}
