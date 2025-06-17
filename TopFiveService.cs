using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CryptoPriceBot.Services
{
    public static class TopFiveService
    {
        public static async Task SendTopFive(ITelegramBotClient bot, Message message)
        {
            Console.WriteLine($"[TOP5] Запит від ChatId: {message.Chat.Id}");

            string url = "https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=market_cap_desc&per_page=5&page=1";

            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; TelegramBot/1.0)");

            try
            {
                string response = await client.GetStringAsync(url);
                dynamic data = JsonConvert.DeserializeObject(response);
                if (data == null)
                {
                    await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Не вдалося отримати інформацію про топ-5 криптовалют.");
                    return;
                }

                string result = "🏆 *Топ-5 криптовалют за капіталізацією:*\n";
                foreach (var coin in data)
                {
                    string name = coin.name;
                    string symbol = coin.symbol;
                    double price = coin.current_price;
                    result += $"• {name} ({symbol.ToUpper()}): {price:F2} USD\n";
                }

                await bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: result,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TOP5 error] {ex.Message}");
                await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Не вдалося завантажити топ-5 криптовалют.");
            }
        }
    }
}
