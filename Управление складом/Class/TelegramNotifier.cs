using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace УправлениеСкладом.Class
{
    public static class TelegramNotifier
    {
        // Укажите здесь ваш токен бота
        private static readonly string BotToken = "8109686508:AAF-AxQDHV2cEIS0BGjIsGSql8LSOmpKOGM";

        // Чат ID для уведомлений о поступлении товара (например, группа сотрудников)
        private static readonly long EmployeeChatId = 974698070; // Замените на реальный ID

        // Чат ID менеджера для уведомлений о товаре, который закончился
        private static readonly long ManagerChatId = 866244297; // Замените на реальный ID

        private static readonly TelegramBotClient _botClient = new TelegramBotClient(BotToken);

        public static async Task SendNotificationAsync(string message, bool toManager = false)
        {
            long chatId = toManager ? ManagerChatId : EmployeeChatId;
            try
            {
                await _botClient.SendTextMessageAsync(chatId, message);
            }
            catch (Exception ex)
            {
                // Можно логировать ошибку, если потребуется
                Console.WriteLine("Ошибка отправки уведомления: " + ex.Message);
            }
        }
    }
}
