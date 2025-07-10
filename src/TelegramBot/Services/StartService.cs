using System.Net.Http;
using Telegram.Bot;
using TelegramBot.Config;
using TelegramBot.Interfaces;

namespace TelegramBot.Services
{
    public class StartService : IStartService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly TelegramBotConfig _config;
        private readonly HttpClient _httpClient;

        public StartService
            (TelegramBotConfig config)
        {
            _config = config;
            _botClient = new TelegramBotClient(_config.Token);
            _httpClient = new HttpClient { BaseAddress = new Uri(_config.ApiBaseUrl) };
        }

        public async Task<bool> ProfileChecker(long chatId, CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<bool>($"/api/profile/ise/{chatId}", ct);

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex}");
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "⚠️ Произошла ошибка",
                    cancellationToken: ct);
                return false;
            }
        }
    }
}
