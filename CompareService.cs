using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CryptoPriceBot.Services
{
    public static class CompareService
    {
        public static async Task SendComparison(ITelegramBotClient bot, Message message)
        {
            var parts = message.Text.Split(' ');
            if (parts.Length != 4)
            {
                await bot.SendTextMessageAsync(message.Chat.Id, "Формат: /compare <crypto1> <crypto2> <date (DD-MM-YYYY)>");
                return;
            }

            string crypto1 = parts[1].ToLower();
            string crypto2 = parts[2].ToLower();
            string date = parts[3];

            if (!DateTime.TryParseExact(date, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                await bot.SendTextMessageAsync(message.Chat.Id, "Невірний формат дати. Використовуйте DD-MM-YYYY.");
                return;
            }

            async Task<double?> GetPrice(string cryptoId)
            {
                try
                {
                    string url = $"https://api.coingecko.com/api/v3/coins/{cryptoId}/history?date={date}&localization=false";
                    using HttpClient client = new();
                    string response = await client.GetStringAsync(url);
                    dynamic data = JsonConvert.DeserializeObject(response);
                    return (double?)data?.market_data?.current_price?.usd;
                }
                catch { return null; }
            }

            double? price1 = await GetPrice(crypto1);
            double? price2 = await GetPrice(crypto2);

            if (price1 == null || price2 == null)
            {
                await bot.SendTextMessageAsync(message.Chat.Id, "Не вдалося отримати дані про одну або обидві криптовалюти.");
                return;
            }

            string comparison = price1 > price2 ?
                $"{crypto1.ToUpper()} був дорожчим за {crypto2.ToUpper()} в {(price1.Value / price2.Value):F2} раз(и)." :
                $"{crypto2.ToUpper()} був дорожчим за {crypto1.ToUpper()} в {(price2.Value / price1.Value):F2} раз(и).";

            string result = $"Дата: {date}\n" +
                            $"{crypto1.ToUpper()}: {price1:F2} USD\n" +
                            $"{crypto2.ToUpper()}: {price2:F2} USD\n\n" +
                            comparison;

            await bot.SendTextMessageAsync(message.Chat.Id, result);
            Console.WriteLine($"[Compare] ✅ {crypto1} vs {crypto2} на {date}");
        }
    }
}
