using DatingBotLibrary.Domain.Entities.Enum;
using Telegram.Bot.Types;

namespace TelegramBot.Interfaces
{
    public interface ICreateProfileService
    {
        Task HandleCreateCommand(long chatId, CancellationToken ct);
        Task HandleCreateInputCommand(long chatId, Message message, CancellationToken ct);
        Task ClearAllStates(long chatId, CancellationToken ct);
    }
}
