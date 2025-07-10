using DatingBotLibrary.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Config;
using TelegramBot.Interfaces;

namespace TelegramBot.Services
{
    public class ProfilesService : IProfilesService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly TelegramBotConfig _config;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<long, List<Profile>> _datingProfiles;
        private readonly Dictionary<long, Profile> _likes;

        public ProfilesService
            (TelegramBotConfig config,
            IHttpClientFactory httpClientFactory,
            [FromKeyedServices("likes")] Dictionary<long, Profile> likes,
            [FromKeyedServices("checkLikes")] Dictionary<long, List<Profile>> datingProfiles)
        {
            _config = config;
            _botClient = new TelegramBotClient(_config.Token);
            _httpClient = httpClientFactory.CreateClient("BotApi");
            _datingProfiles = datingProfiles;
            _likes = likes;
        }


        public async Task StartProfileViewing(long chatId, CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/profile/s/{chatId}", ct);

                if (!response.IsSuccessStatusCode)
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Ошибка при загрузке анкет",
                        cancellationToken: ct);
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Raw API response: {content}");


                var profiles = JsonSerializer.Deserialize<List<Profile>>(
                    content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Profile>();

                if (!profiles.Any())
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Нет подходящих анкет. Попробуйте позже или измените параметры поиска.",
                        cancellationToken: ct);
                    return;
                }

                _datingProfiles[chatId] = profiles;
                await ShowRandomProfile(chatId, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex}");
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "⚠️ Ошибка при загрузке анкет",
                    cancellationToken: ct);
            }
        }
        public async Task ShowRandomProfile(long chatId, CancellationToken ct)
        {
            if (!_datingProfiles.TryGetValue(chatId, out var profiles) || !profiles.Any())
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
                    text: "Анкеты закончились",
                    replyMarkup: replyKeyboard,
                    cancellationToken: ct);
                return;
            }

            var profile = profiles.First();
            _datingProfiles[chatId] = profiles.Skip(1).ToList();

            _likes[chatId] = profile;

            var caption = $"{profile.Name}, {profile.Age}, {profile.City}";
            if (!string.IsNullOrEmpty(profile.Bio) && !profile.Bio.Equals("Не указано"))
            {
                caption += $" - {profile.Bio}";
            }


            var replyMarkup = new ReplyKeyboardMarkup(new[]
            {

            new[] { new KeyboardButton("❤️ Лайк"), new KeyboardButton("👎 Дизлайк") },
            new[] { new KeyboardButton("🚫 Прекратить просмотр") }

            })
            {
                ResizeKeyboard = true
            };


            if (profile.Photos.Any() || profile.Videos.Any())
            {
                var mediaGroup = new List<IAlbumInputMedia>();


                foreach (var photo in profile.Photos.Take(10))
                {
                    mediaGroup.Add(new InputMediaPhoto(new InputFileId(photo.FileId))
                    {
                        Caption = mediaGroup.Count == 0 ? caption : null
                    });
                }


                foreach (var video in profile.Videos.Take(10 - mediaGroup.Count))
                {
                    mediaGroup.Add(new InputMediaVideo(new InputFileId(video.FileId))
                    {
                        Caption = mediaGroup.Count == 0 ? caption : null
                    });
                }

                if (mediaGroup.Count > 0)
                {
                    await _botClient.SendMediaGroup(
                        chatId: chatId,
                        media: mediaGroup,
                        cancellationToken: ct);


                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Выберите действие:",
                        replyMarkup: replyMarkup,
                        cancellationToken: ct);
                }
            }
            else
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: caption,
                    replyMarkup: replyMarkup,
                    cancellationToken: ct);
            }
        }

        public async Task SendUserProfile(long chatId, CancellationToken ct)
        {
            try
            {

                var response = await _httpClient.GetAsync($"/api/profile/{chatId}", ct);
                var json = await response.Content.ReadAsStringAsync();

                var profile = JsonSerializer.Deserialize<Profile>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                });

                if (profile == null)
                {
                    await _botClient.SendMessage(chatId, "Профиль не найден", cancellationToken: ct);
                    return;
                }

                var caption = profile.Bio != null && !profile.Bio.Equals("Не указано", StringComparison.OrdinalIgnoreCase)
                    ? $"{profile.Name}, {profile.Age}, {profile.City} – {profile.Bio}"
                    : $"{profile.Name}, {profile.Age}, {profile.City}";


                var mediaGroup = new List<IAlbumInputMedia>();


                foreach (var photo in profile.Photos)
                {
                    mediaGroup.Add(new InputMediaPhoto(new InputFileId(photo.FileId))
                    {
                        Caption = mediaGroup.Count == 0 ? caption : null
                    });
                }


                foreach (var video in profile.Videos)
                {
                    mediaGroup.Add(new InputMediaVideo(new InputFileId(video.FileId))
                    {
                        Caption = mediaGroup.Count == 0 ? caption : null
                    });
                }

                if (mediaGroup.Count > 0)
                {
                    await _botClient.SendMediaGroup(
                        chatId: chatId,
                        media: mediaGroup,
                        cancellationToken: ct);


                    var replyKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                            new[]
                            {
                            new KeyboardButton("🚀 Смотреть анкеты"),
                            new KeyboardButton("📝 Заполнить анкету заново"),
                            new KeyboardButton("💤")
                            }
                            })
                    {
                        ResizeKeyboard = true
                    };


                    await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Выберите действие:",
                    replyMarkup: replyKeyboard,
                    cancellationToken: ct);
                }
                else
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: caption,
                        cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex}");
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "⚠️ Ошибка при загрузке профиля",
                    cancellationToken: ct);
            }
        }
    }
}
