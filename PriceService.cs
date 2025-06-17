using Telegram.Bot;
using Telegram.Bot.Types;
using Newtonsoft.Json;
using System.Globalization;

namespace CryptoPriceBot.Services
{
    public static class PriceService
    {
        public static async Task GetCryptoPrice(ITelegramBotClient bot, Message message)
        {
            var parts = message.Text.Split(' ');
            if (parts.Length < 3)
            {
                await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Використовуйте: /price <crypto_symbol> <date>");
                return;
            }

            string cryptoSymbol = parts[1];
            string date = parts[2];

            if (!DateTime.TryParseExact(date, "dd-MM-yyyy", null, DateTimeStyles.None, out DateTime parsedDate) ||
                parsedDate < DateTime.Now.AddDays(-365) || parsedDate > DateTime.Now)
            {
                await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Дата має бути у форматі DD-MM-YYYY і не старіша за 365 днів.");
                return;
            }

            string url = $"https://api.coingecko.com/api/v3/coins/{cryptoSymbol}/history?date={date}&localization=false";
            HttpClient client = new();
            try
            {
                string response = await client.GetStringAsync(url);
                dynamic data = JsonConvert.DeserializeObject(response);
                var price = data?.market_data?.current_price?.usd;
                if (price == null)
                    throw new Exception();

                Console.WriteLine($"[PRICE] {cryptoSymbol.ToUpper()} on {date} = {price} USD");

                await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: $"Ціна {cryptoSymbol.ToUpper()} на {date}: {price} USD");
                HistoryService.AddToHistory(message.Chat.Id, message.Text);
            }
            catch
            {
                Console.WriteLine($"[PRICE ERROR] Failed to get price for {cryptoSymbol} on {date}");
                await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Не вдалося отримати ціну криптовалюти.");
            }
        }
    }
}
