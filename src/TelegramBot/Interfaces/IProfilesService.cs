namespace TelegramBot.Interfaces
{
    public interface IProfilesService
    {
        Task StartProfileViewing(long chatId, CancellationToken ct);
        Task ShowRandomProfile(long chatId, CancellationToken ct);
        Task SendUserProfile(long chatId, CancellationToken ct);
    }
}
