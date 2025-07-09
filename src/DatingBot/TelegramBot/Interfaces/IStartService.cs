namespace TelegramBot.Interfaces
{
    public interface IStartService
    {
        Task<bool> ProfileChecker(long chatId, CancellationToken ct);
    }
}
