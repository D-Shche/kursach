using Newtonsoft.Json;
using ScottPlot;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CryptoPriceBot.Services
{
    public static class ChartService
    {
        public static async Task SendCryptoChartAsync(ITelegramBotClient bot, Message message)
        {
            var parts = message.Text.Split(' ');
            if (parts.Length < 3 || !int.TryParse(parts[2], out int days) || days < 1 || days > 365)
            {
                await bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Використовуйте: /chart <crypto_symbol> <кількість_днів> (1–365)"
                );
                return;
            }

            string cryptoSymbol = parts[1].ToLower();
            string url = $"https://api.coingecko.com/api/v3/coins/{cryptoSymbol}/market_chart?vs_currency=usd&days={days}";

            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync(url);
                dynamic data = JsonConvert.DeserializeObject(response);

                if (data?.prices == null || data.prices.Count == 0)
                {
                    await bot.SendTextMessageAsync(message.Chat.Id, "API не повернуло цін.");
                    return;
                }

                var x = new List<double>();
                var y = new List<double>();

                foreach (var point in data.prices)
                {
                    long unixTime = point[0];
                    double price = point[1];
                    DateTime time = DateTimeOffset.FromUnixTimeMilliseconds(unixTime).DateTime;

                    x.Add(time.ToOADate());
                    y.Add(price);
                }

                if (x.Count == 0 || y.Count == 0)
                {
                    await bot.SendTextMessageAsync(message.Chat.Id, "Недостатньо даних для побудови графіка.");
                    return;
                }

                // Побудова графіка
                var plt = new ScottPlot.Plot(600, 400);
                plt.AddScatter(x.ToArray(), y.ToArray());
                plt.Title($"Ціна {cryptoSymbol.ToUpper()} за {days} дн.");
                plt.XLabel("Дата");
                plt.YLabel("USD");
                plt.XAxis.DateTimeFormat(true);

                // Збереження графіка у файл
                string filePath = "chart.png";
                plt.SaveFig(filePath);

                // Перевірка існування файлу
                if (!System.IO.File.Exists(filePath) || new System.IO.FileInfo(filePath).Length == 0)
                {
                    await bot.SendTextMessageAsync(message.Chat.Id, "Не вдалося створити графік.");
                    return;
                }

                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var inputFile = new InputFileStream(stream, "chart.png");

                await bot.SendPhotoAsync(
                    chatId: message.Chat.Id,
                    photo: inputFile,
                    caption: $"Ціна {cryptoSymbol.ToUpper()} за останні {days} днів."
                );

                stream.Close();
                System.IO.File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Chart error: " + ex.Message);
                await bot.SendTextMessageAsync(message.Chat.Id, "Помилка побудови графіка. Перевірте назву монети.");
            }
        }
    }
}
