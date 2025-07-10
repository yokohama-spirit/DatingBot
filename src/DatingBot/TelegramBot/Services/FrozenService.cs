using DatingBotLibrary.Domain.Entities;
using Telegram.Bot;
using TelegramBot.Config;
using TelegramBot.Interfaces;
using System.Text.Json.Serialization;
using System.Text.Json;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot.Services
{
    public class FrozenService : IFrozenService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly TelegramBotConfig _config;
        private readonly HttpClient _httpClient;

        public FrozenService
            (TelegramBotConfig config)
        {
            _config = config;
            _botClient = new TelegramBotClient(_config.Token);
            _httpClient = new HttpClient { BaseAddress = new Uri(_config.ApiBaseUrl) };
        }

        public async Task FrozenHandle(long chatId, CancellationToken ct)
        {

            try
            {
                var response = await _httpClient.GetFromJsonAsync<bool>($"/api/profile/frozen/{chatId}", ct);

                if (!response)
                {
                    var replyKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "Разморозить анкету 😴" }
                        })
                    {
                        ResizeKeyboard = true
                    };

                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Ваша анкета уже заморожена!",
                        replyMarkup: replyKeyboard,
                        cancellationToken: ct);
                }
                else
                {
                    var replyKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "Разморозить анкету 😴" }
                        })
                    {
                        ResizeKeyboard = true
                    };

                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Ваша анкета успешно заморожена!",
                        replyMarkup: replyKeyboard,
                        cancellationToken: ct);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex}");
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "⚠️ Произошла ошибка",
                    cancellationToken: ct);
            }
        }

        public async Task<bool> IsFrozen(long chatId, CancellationToken ct)
        {
            var response = await _httpClient.GetAsync($"/api/profile/{chatId}", ct);
            var json = await response.Content.ReadAsStringAsync();

            var me = JsonSerializer.Deserialize<Profile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve
            });

            if (me == null)
            {
                await _botClient.SendMessage(chatId, "Профиль не найден", cancellationToken: ct);
                return false;
            }

            if (me.isFrozen == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task UnfrozenHandle(long chatId, CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<bool>($"/api/profile/unfrozen/{chatId}", ct);

                if (response)
                {
                    var replyKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                    new[]
                    {
                    new KeyboardButton("🚀 Смотреть анкеты"),
                    new KeyboardButton("👤 Моя анкета"),
                    new KeyboardButton("💤")
                    }
                    })
                    {
                        ResizeKeyboard = true
                    };


                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Ваша анкета успешно разморожена!",
                        replyMarkup: replyKeyboard,
                        cancellationToken: ct);

                }
                else
                {
                    var replyKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                    new[]
                    {
                    new KeyboardButton("🚀 Смотреть анкеты"),
                    new KeyboardButton("👤 Моя анкета"),
                    new KeyboardButton("💤")
                    }
                    })
                    {
                        ResizeKeyboard = true
                    };


                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Вашу анкету нельзя разморозить, ведь она не заморожена🤥",
                        replyMarkup: replyKeyboard,
                        cancellationToken: ct);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex}");
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "⚠️ Произошла ошибка",
                    cancellationToken: ct);
            }
        }
    }
}
