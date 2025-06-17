using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Data.SQLite;
using Newtonsoft.Json;
using Dapper;
using ScottPlot;
using CryptoPriceBot.Services;
namespace CryptoPriceBot;

class Program
{
    private static readonly string botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
    private static readonly TelegramBotClient botClient = new(botToken);
    private static readonly string connectionString = "Data Source=crypto_bot.db;Version=3;";

    static async Task Main(string[] args)
    {
        InitializeDatabase();
        StartPriceAlertChecker();
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions(); 

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Bot started: @{me.Username}");
        Console.ReadLine();


    }
    private static Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Bot error: {exception.Message}");
        return Task.CompletedTask;
    }
    private static void StartPriceAlertChecker()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    using var connection = new SQLiteConnection(connectionString);
                    connection.Open();
                    var alerts = connection.Query("SELECT * FROM Alerts").ToList();

                    foreach (var alert in alerts)
                    {
                        try
                        {
                            string url = $"https://api.coingecko.com/api/v3/simple/price?ids={alert.CryptoSymbol}&vs_currencies=usd";
                            HttpClient client = new();
                            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; TelegramBot/1.0)");
                            var response = await client.GetStringAsync(url);
                            dynamic data = JsonConvert.DeserializeObject(response);
                            double? currentPrice = data?[alert.CryptoSymbol]?.usd;

                            Console.WriteLine($"[AlertCheck] {alert.CryptoSymbol} = {currentPrice}, target {alert.Direction} {alert.TargetPrice}");

                            if (currentPrice != null)
                            {
                                bool shouldNotify = (alert.Direction == "above" && currentPrice >= alert.TargetPrice) ||
                                                    (alert.Direction == "below" && currentPrice <= alert.TargetPrice);
                                if (shouldNotify)
                                {
                                    string msg = $"Ціна {alert.CryptoSymbol.ToUpper()} досягла {currentPrice:F4} USD (умова: {alert.Direction} {alert.TargetPrice})";

                                    Console.WriteLine($"[ALERT TRIGGERED] => {msg} для чату {alert.ChatId}");

                                    await botClient.SendTextMessageAsync(
                                        chatId: (long)alert.ChatId,
                                        text: msg
                                    );

                                    connection.Execute("DELETE FROM Alerts WHERE ChatId = @ChatId AND CryptoSymbol = @CryptoSymbol",
                                        new { alert.ChatId, alert.CryptoSymbol });
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[No price] Didnt get price for {alert.CryptoSymbol}");
                            }

                            await Task.Delay(2000); // більша затримка між запитами
                        }
                        catch (Exception exInner)
                        {
                            Console.WriteLine($"[AlertChecker inner error] {exInner.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("AlertChecker error: " + ex.Message);
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        });
    }


    private static void InitializeDatabase()
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        connection.Execute(@"
    CREATE TABLE IF NOT EXISTS Favorites (
        ChatId INTEGER NOT NULL,
        CryptoSymbol TEXT NOT NULL,
        Date TEXT NOT NULL,
        PRIMARY KEY(ChatId, CryptoSymbol, Date)
    );
    CREATE TABLE IF NOT EXISTS QueryHistory (
        ChatId INTEGER NOT NULL,
        QueryText TEXT NOT NULL,
        DateTime DATETIME NOT NULL,
        PRIMARY KEY(ChatId, DateTime)
    );
    CREATE TABLE IF NOT EXISTS Alerts (
        ChatId INTEGER NOT NULL,
        CryptoSymbol TEXT NOT NULL,
        Direction TEXT NOT NULL,
        TargetPrice REAL NOT NULL
    );");
    }
    private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text == null)
            return;

        var message = update.Message;
        var text = message.Text;

        // ✅ ЛОГУЄМО ВСЕ, навіть якщо команда не обробиться
        Console.WriteLine($"[Command Attempt] ChatId: {message.Chat.Id}, Text: {text}");

        try
        {
            if (text.StartsWith("/price")) await PriceService.GetCryptoPrice(bot, message);
            else if (text.StartsWith("/history")) await HistoryService.ShowHistory(bot, message);
            else if (text.StartsWith("/favorite")) await FavoritesService.ShowFavorites(bot, message);
            else if (text.StartsWith("/addfavorite")) await FavoritesService.AddFavorite(bot, message);
            else if (text.StartsWith("/removefavorite")) await FavoritesService.RemoveFavorite(bot, message);
            else if (text.StartsWith("/help")) await HelpService.ShowHelp(bot, message);
            else if (text.StartsWith("/chart")) await ChartService.SendCryptoChartAsync(bot, message);
            else if (text.StartsWith("/compare")) await CompareService.SendComparison(bot, message);
            else if (text.StartsWith("/top5")) await TopFiveService.SendTopFive(bot, message);
            else if (text.StartsWith("/alert")) await AlertService.AddAlert(bot, message);
            else if (text.StartsWith("/clearalerts")) await AlertService.ClearAlerts(bot, message);
            else
            {
                Console.WriteLine($"[Unknown command] \"{text}\" не розпізнано.");
                await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Скористайтесь /help для списку команд.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Exception] Error while processing \"{text}\" — {ex.Message}");
            await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Сталася помилка. Спробуйте пізніше.");
        }
    }



    private static async Task ShowCommands(ITelegramBotClient bot, Message message)
    {
        var keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(new[]
        {
        new[] { new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("/price"), new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("/chart") },
        new[] { new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("/alert"), new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("/favorite") },
        new[] { new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("/history"), new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("/help") }
    })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        string welcomeText =
            "👋 Вітаю! Я крипто-бот, який допоможе дізнатись актуальні ціни, створити алерти та зберегти улюблені монети.\n\n" +
            "⬇️ Обери команду нижче або надішли власну. Наприклад:\n" +
            "• `/price btc 15-06-2024`\n" +
            "• `/alert eth above 4000`\n\n" +
            "• `/compare btc eth 15-06-2024`\n\n\n"+
            "📌 Надішліть /help для перегляду всіх команд.";
            

        await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: welcomeText,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: keyboard
        );
    }

}
