namespace TelegramBot.Interfaces
{
    public interface IFrozenService
    {
        Task<bool> IsFrozen(long chatId, CancellationToken ct);
        Task FrozenHandle(long chatId, CancellationToken ct);
        Task UnfrozenHandle(long chatId, CancellationToken ct);
    }
}
