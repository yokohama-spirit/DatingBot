using DatingBotLibrary.Domain.Entities;
using Telegram.Bot;
using TelegramBot.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot.Services
{
    public class LikesService : ILikesService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly IProfilesService _profile;
        private readonly Dictionary<long, Profile> _likes;
        private readonly Dictionary<long, Profile> _mutually;
        private readonly Dictionary<long, List<Profile>> _checkLikes;

        public LikesService(
            ITelegramBotClient botClient,
            IHttpClientFactory httpClientFactory,
            IProfilesService profile,
            [FromKeyedServices("likes")] Dictionary<long, Profile> likes,
            [FromKeyedServices("mutually")] Dictionary<long, Profile> mutually,
            [FromKeyedServices("checkLikes")] Dictionary<long, List<Profile>> checkLikes)
        {
            _botClient = botClient;
            _httpClient = httpClientFactory.CreateClient("BotApi");
            _likes = likes;
            _mutually = mutually;
            _checkLikes = checkLikes;
            _profile = profile;
        }


        public async Task LikesHandler(long chatId, CancellationToken ct)
        {

            if (_likes.TryGetValue(chatId, out var profile))
            {
                try
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
                        return;
                    }

                    var putResponse = await _httpClient.PutAsync(
                        $"/api/likes/u/{chatId}/{profile.ChatId}",
                        null,
                        ct);

                    if (putResponse.IsSuccessStatusCode)
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Лайк поставлен!",
                            cancellationToken: ct);
                        _likes.Remove(chatId);
                    }
                    else
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "⚠️ Ошибка при сохранении лайка",
                            cancellationToken: ct);
                    }


                    var replyKeyboard = new ReplyKeyboardMarkup(new[]
{
                    new[]
                    {
                    new KeyboardButton("Посмотреть"),
                    new KeyboardButton("Неинтересно")
                    }
                    })
                    {
                        ResizeKeyboard = true
                    };

                    var countResponse = await _httpClient.GetFromJsonAsync<decimal>($"/api/likes/count/{profile.ChatId}", ct);

                    var count = (int)countResponse;

                    if (count == 1)
                    {
                        await _botClient.SendMessage(
                        chatId: profile.ChatId,
                        text: "Вами кто-то заинтересовался!",
                        replyMarkup: replyKeyboard,
                        cancellationToken: ct);
                    }
                    else
                    {
                        await _botClient.SendMessage(
                        chatId: profile.ChatId,
                        text: $"Вами заинтересовалось {countResponse} человека! Скорее узнай кто это!",
                        replyMarkup: replyKeyboard,
                        cancellationToken: ct);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {ex}");
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "⚠️ Ошибка",
                        cancellationToken: ct);
                }
                finally
                {
                    await _profile.ShowRandomProfile(chatId, ct);
                }

            }
        }



        public async Task StartLikesViewing(long chatId, CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/likes/s/{chatId}", ct);

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

                _checkLikes[chatId] = profiles;
                await ShowLikesProfiles(chatId, ct);
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
        public async Task ShowLikesProfiles(long chatId, CancellationToken ct)
        {
            if (!_checkLikes.TryGetValue(chatId, out var profiles) || !profiles.Any())
            {
                var replyLikesMarkup = new ReplyKeyboardMarkup(new[]
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
                    replyMarkup: replyLikesMarkup,
                    cancellationToken: ct);
                return;
            }

            var profile = profiles.First();
            _checkLikes[chatId] = profiles.Skip(1).ToList();

            _mutually[chatId] = profile;

            var caption = $"{profile.Name}, {profile.Age}, {profile.City}";
            if (!string.IsNullOrEmpty(profile.Bio) && !profile.Bio.Equals("Не указано"))
            {
                caption += $" - {profile.Bio}";
            }


            var replyMarkup = new ReplyKeyboardMarkup(new[]
{
                    new[]
                    {
                    new KeyboardButton("❤️ Взаимно"),
                    new KeyboardButton("💔 Невзаимно")
                    }
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




        public async Task MutuallyHandler(long chatId, CancellationToken ct)
        {
            if (!_mutually.TryGetValue(chatId, out var profile)) return;

            try
            {
                var response = await _httpClient.GetAsync($"/api/profile/{chatId}", ct);
                var json = await response.Content.ReadAsStringAsync();

                var currentUser = JsonSerializer.Deserialize<Profile>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                });


                if (currentUser == null || profile == null)
                {
                    await _botClient.SendMessage(chatId, "Профиль не найден", cancellationToken: ct);
                    return;
                }


                var deleteResponse = await _httpClient.PutAsync(
                    $"/api/likes/d/{currentUser.UserId}/{profile.UserId}",
                    null,
                    ct);

                if (!deleteResponse.IsSuccessStatusCode)
                {
                    await _botClient.SendMessage(chatId, "Ошибка при обработке", cancellationToken: ct);
                    return;
                }




                string userLink = $"tg://user?id={profile.UserId}";
                string htmlMessage =
                    $"Отлично! Начинай общаться с <a href=\"{userLink}\">{HtmlEscape(profile.Name)}</a>!";

                await _botClient.SendMessage(
                    chatId: chatId,
                    text: htmlMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);


                string otherUserLink = $"tg://user?id={currentUser.UserId}";
                string otherHtmlMessage =
                    $"Вам ответили взаимностью! Начинай общаться с <a href=\"{otherUserLink}\">{HtmlEscape(currentUser.Name)}</a>!";

                var replyKeyboard = new ReplyKeyboardMarkup(new[]
                {
                new[]
                {
                new KeyboardButton("🚀 Смотреть анкеты"),
                new KeyboardButton("👤 Моя анкета")
                }
                })
                {
                    ResizeKeyboard = true
                };

                await _botClient.SendMessage(
                    chatId: profile.ChatId,
                    text: otherHtmlMessage,
                    parseMode: ParseMode.Html,
                    replyMarkup: replyKeyboard,
                    cancellationToken: ct);


            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex}");
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "⚠️ Произошла ошибка",
                    cancellationToken: ct);
            }
            finally
            {
                _mutually.Remove(chatId);
            }
        }


        private static string HtmlEscape(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        public async Task NonMutuallyHandler(long chatId, CancellationToken ct)
        {
            if (!_mutually.TryGetValue(chatId, out var profile)) return;

            try
            {
                var response = await _httpClient.GetAsync($"/api/profile/{chatId}", ct);
                var json = await response.Content.ReadAsStringAsync();

                var currentUser = JsonSerializer.Deserialize<Profile>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                });

                var deleteResponse = await _httpClient.PutAsync(
                    $"/api/likes/d/{currentUser.UserId}/{profile.UserId}",
                    null,
                    ct);

                if (!deleteResponse.IsSuccessStatusCode)
                {
                    await _botClient.SendMessage(chatId, "Ошибка при обработке", cancellationToken: ct);
                    return;
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
            finally
            {
                _mutually.Remove(chatId);
                await ShowLikesProfiles(chatId, ct);
            }
        }

    }
}
