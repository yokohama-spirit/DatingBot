namespace TelegramBot.Interfaces.Other
{
    public interface IHandleStartCommand
    {
        Task StartCommand(long chatId, CancellationToken ct);
    }
}
