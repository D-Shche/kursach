using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CryptoPriceBot.Services
{
    public static class HelpService
    {
        public static async Task ShowHelp(ITelegramBotClient bot, Message message)
        {
            string helpText =
                "🧾 *Доступні команди:*\n\n" +
                "💰 `/price <symbol> <дата>` – дізнатись ціну на дату (формат: DD-MM-YYYY)\n" +
                "📊 `/chart <symbol> <днів>` – графік змін за останні дні (1–365)\n" +
                "📉 `/compare <symbol1> <symbol2> <дата>` – порівняння цін монет у минулому\n" +
                "🛎 `/alert <symbol> <above|below> <ціна>` – створити алерт на ціну\n" +
                "❌ `/clearalerts` – видалити всі свої алерти\n\n" +
                "⭐ `/favorite` – показати улюблені монети\n" +
                "➕ `/addfavorite <symbol> <дата>` – додати до улюблених\n" +
                "➖ `/removefavorite <symbol> <дата>` – прибрати з улюблених\n\n" +
                "🕘 `/history` – показати останні 5 запитів\n" +
                "ℹ️ `/help` – показати це меню знову";

            Console.WriteLine($"[HELP] User requested help menu");

            await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: helpText,
                parseMode: ParseMode.Markdown
            );
        }

    }
}
