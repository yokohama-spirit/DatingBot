using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Config;
using TelegramBot.Interfaces.Other;

namespace TelegramBot.Services.Other
{
    public class HandleStartCommand : IHandleStartCommand
    {
        private readonly ITelegramBotClient _botClient;
        private readonly TelegramBotConfig _config;


        public HandleStartCommand
            (TelegramBotConfig config)
        {
            _config = config;
            _botClient = new TelegramBotClient(_config.Token);
        }

        public async Task StartCommand(long chatId, CancellationToken ct)
        {
            var chat = await _botClient.GetChat(chatId, ct);

            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
            new KeyboardButton[] { "☃️ Создать анкету" }
            })
            {
                ResizeKeyboard = true
            };

            await _botClient.SendMessage(
                chatId: chatId,
                text: $"Привет, {chat.FirstName ?? "друг"}! Я - бот для знакомств!",
                replyMarkup: replyKeyboard,
                cancellationToken: ct);
        }
    }
}
