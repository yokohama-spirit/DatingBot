namespace TelegramBot.Interfaces
{
    public interface ILikesService
    {
        Task LikesHandler(long chatId, CancellationToken ct);
        Task StartLikesViewing(long chatId, CancellationToken ct);
        Task ShowLikesProfiles(long chatId, CancellationToken ct);
        Task MutuallyHandler(long chatId, CancellationToken ct);
        Task NonMutuallyHandler(long chatId, CancellationToken ct);
    }
}
