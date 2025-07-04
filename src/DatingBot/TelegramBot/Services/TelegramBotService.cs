using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using TelegramBot.Interfaces;
using TelegramBot.Config;
using TelegramBot.Config.State;
using Telegram.Bot.Types.ReplyMarkups;
using DatingBotLibrary.Domain.Entities;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Text.Json.Serialization;
using System.Text.Json;
using Video = DatingBotLibrary.Domain.Entities.Video;
using DatingBotLibrary.Domain.Entities.Enum;

namespace TelegramBot.Services
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly TelegramBotConfig _config;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<long, CreateProfileState> _state;
        private readonly Dictionary<long, List<Profile>> _datingProfiles;
        private readonly Dictionary<long, Profile> _likes;
        private readonly Dictionary<long, Profile> _mutually;
        private readonly Dictionary<long, List<Profile>> _checkLikes;

        public TelegramBotService
            (TelegramBotConfig config)
        {
            _config = config;
            _botClient = new TelegramBotClient(_config.Token);
            _httpClient = new HttpClient { BaseAddress = new Uri(_config.ApiBaseUrl) };
            _state = new Dictionary<long, CreateProfileState>();
            _datingProfiles = new Dictionary<long, List<Profile>>();
            _checkLikes = new Dictionary<long, List<Profile>>();
            _likes = new Dictionary<long, Profile>();
            _mutually = new Dictionary<long, Profile>();
        }


        //--------------------------------------START------------------------------------------

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cancellationToken
            );

            Console.WriteLine("Бот запущен. Нажмите Ctrl+C для остановки...");
        }

        //--------------------------------------UPDATE------------------------------------------

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            try
            {
                if (update.Message is { } message)
                {
                    long chatId = message.Chat.Id;

                    if (_state.TryGetValue(chatId, out var state))
                    {
                        await HandleCreateInputCommand(chatId, message, ct);
                        return;
                    }

                    switch (message.Text)
                    {
                        case "/start":
                        case "Главное меню":
                            await HandleStartCommand(chatId, ct);
                            break;

                        case "☃️ Создать анкету":
                            await HandleCreateCommand(chatId, ct);
                            break;

                        case "📝 Заполнить анкету заново":
                            await HandleCreateCommand(chatId, ct);
                            break;

                        case "👤 Моя анкета":
                            await SendUserProfile(chatId, ct);
                            break;

                        case "🚀 Смотреть анкеты":
                            await StartProfileViewing(chatId, ct);
                            break;

                        case "❤️ Лайк":
                            await LikesHandler(chatId, ct);
                            await ShowRandomProfile(chatId, ct);
                            break;

                        case "👎 Дизлайк":
                            await ShowRandomProfile(chatId, ct);
                            break;

                        case "Посмотреть":
                            await StartLikesViewing(chatId, ct);
                            break;

                        case "❤️ Взаимно":
                            await MutuallyHandler(chatId, ct);
                            await ShowLikesProfiles(chatId, ct);
                            break;

                        case "💔 Невзаимно":
                            await NonMutuallyHandler(chatId, ct);
                            break;

                        case "🚫 Прекратить просмотр":

                            var replyMediaKeyboard = new ReplyKeyboardMarkup(new[]
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
                                chatId: chatId,
                                text: "Просмотр анкет завершен",
                                replyMarkup: new ReplyKeyboardRemove(),
                                cancellationToken: ct);

                            await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Выберите действие:",
                            replyMarkup: replyMediaKeyboard,
                            cancellationToken: ct);
                            break;

                        default:
                            await _botClient.SendMessage(
                                chatId: chatId,
                                text: "Не понимаю, о чем ты 😅",
                                cancellationToken: ct);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex}");
            }
        }
        private async Task StartProfileViewing(long chatId, CancellationToken ct)
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
        private async Task ShowRandomProfile(long chatId, CancellationToken ct)
        {
            if (!_datingProfiles.TryGetValue(chatId, out var profiles) || !profiles.Any())
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Анкеты закончились",
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



        private async Task LikesHandler(long chatId, CancellationToken ct)
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

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {ex}");
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "⚠️ Ошибка",
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

                await _botClient.SendMessage(
                chatId: profile.ChatId,
                text: "Вами кто-то заинтересовался!",
                replyMarkup: replyKeyboard,
                cancellationToken: ct);
            }
        }















        private async Task StartLikesViewing(long chatId, CancellationToken ct)
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
        private async Task ShowLikesProfiles(long chatId, CancellationToken ct)
        {
            if (!_checkLikes.TryGetValue(chatId, out var profiles) || !profiles.Any())
            {
                var replyLikesMarkup = new ReplyKeyboardMarkup(new[]
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




        private async Task MutuallyHandler(long chatId, CancellationToken ct)
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






        private async Task NonMutuallyHandler(long chatId, CancellationToken ct)
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




        //--------------------------------------START COMMAND------------------------------------------

        public async Task HandleStartCommand(long chatId, CancellationToken ct)
        {
            var chat = await _botClient.GetChat(chatId, ct);


            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
            new KeyboardButton[] { "☃️ Создать анкету", "👤 Моя анкета" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false 
            };

            Console.WriteLine(chatId);


            await _botClient.SendMessage(
                chatId: chatId,
                text: $"Привет, {chat.FirstName ?? "друг"}! Я - бот для знакомств!",
                replyMarkup: replyKeyboard,
                cancellationToken: ct);
        }

        //--------------------------------------CREATE------------------------------------------

        public async Task HandleCreateCommand(long chatId, CancellationToken ct)
        {
            _state[chatId] = new CreateProfileState
            {
                Step = 1,
                PhotoFileId = new List<string>(),
                VideoFileId = new List<string>()
            };

            var removeKeyboard = new ReplyKeyboardRemove();

            await _botClient.SendMessage(
                chatId: chatId,
                text: "Как вас зовут?",
                replyMarkup: removeKeyboard,
                cancellationToken: ct);
        }
        public async Task HandleCreateInputCommand(long chatId, Message message, CancellationToken ct)
        {
            if (!_state.TryGetValue(chatId, out var state))
                return;

            string text = message.Text;
            long userId = message.From.Id;

            switch (state.Step)
            {
                case 1:
                    state.Name = text;
                    state.Step = 2;


                    await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Сколько вам лет?",
                    cancellationToken: ct);
                    
                    break;

                case 2 when int.TryParse(text, out var count):
                    state.Age = count;
                    state.Step = 3;


                    await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Назовите свой город:",
                    cancellationToken: ct);

                    break;

                case 3:
                    state.City = text;
                    state.Step = 4;

                    var replyKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                    new KeyboardButton[] { "Пропустить" }
                    })
                    {
                        ResizeKeyboard = true
                    };
                    await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Добавьте описание, которое лучше вас раскроет для других людей!",
                    replyMarkup: replyKeyboard,
                    cancellationToken: ct);

                    state.Step = 4;

                    break;

                case 4:
                    var removeKeyboard = new ReplyKeyboardRemove();

                    state.Desc = text.Equals("Пропустить", StringComparison.OrdinalIgnoreCase)
                    ? "Не указано"
                    : text;

                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: text.Equals("Пропустить", StringComparison.OrdinalIgnoreCase)
                            ? "Описание пропущено"
                            : "Описание сохранено",
                        replyMarkup: removeKeyboard,
                        cancellationToken: ct);


                    var replyGenderKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                    new[] //сверху
                    {
                    new KeyboardButton("Девушка"),
                    new KeyboardButton("Парень")
                    }
                    })
                    {
                        ResizeKeyboard = true
                    };

                    await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Какого вы пола?",
                    replyMarkup: replyGenderKeyboard,
                    cancellationToken: ct);

                    state.Step = 5;

                    break;



                case 5:
                    if (message.Text?.Equals("Девушка", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        state.Gender = DatingBotLibrary.Domain.Entities.Enum.Gender.Female;

                        var replyInterestsKeyboard = new ReplyKeyboardMarkup(new[]
                        {
                        new[] //сверху
                        {
                        new KeyboardButton("Девушки"),
                        new KeyboardButton("Парни")
                        }
                        })
                        {
                            ResizeKeyboard = true
                        };

                        await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Кто вам интересен?",
                        replyMarkup: replyInterestsKeyboard,
                        cancellationToken: ct);

                        state.Step = 6;
                    }
                    else if (message.Text?.Equals("Парень", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        state.Gender = DatingBotLibrary.Domain.Entities.Enum.Gender.Male;

                        var replyInterestsKeyboard = new ReplyKeyboardMarkup(new[]
                        {
                        new[] //сверху
                        {
                        new KeyboardButton("Девушки"),
                        new KeyboardButton("Парни")
                        }
                        })
                        {
                            ResizeKeyboard = true
                        };

                        await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Кто вам интересен?",
                        replyMarkup: replyInterestsKeyboard,
                        cancellationToken: ct);

                        state.Step = 6;
                    }
                    else
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Пожалуйста, выберите свой пол.",
                            cancellationToken: ct);
                    }
                    break;

                case 6:
                    if (message.Text?.Equals("Девушки", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        state.InInterests = DatingBotLibrary.Domain.Entities.Enum.Gender.Female;

                        var removeFemaleKeyboard = new ReplyKeyboardRemove();

                        await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Отправьте фотографию/видео для своей анкеты.",
                        replyMarkup: removeFemaleKeyboard,
                        cancellationToken: ct);

                        state.Step = 7;
                    }
                    else if (message.Text?.Equals("Парни", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        state.InInterests = DatingBotLibrary.Domain.Entities.Enum.Gender.Male;

                        var removeMaleKeyboard = new ReplyKeyboardRemove();

                        await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Отправьте фотографию/видео для своей анкеты.",
                        replyMarkup: removeMaleKeyboard,
                        cancellationToken: ct);

                        state.Step = 7;
                    }
                    else
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Пожалуйста, выберите свой пол.",
                            cancellationToken: ct);
                    }
                    break;


                case 7:
                    if (message.Type == MessageType.Photo && message.Photo != null && message.Photo.Length > 0)
                    {
                        var photo = message.Photo.Last();
                        state.PhotoFileId?.Add(photo.FileId);
                        state.Step = 8;



                        var replyMediaKeyboard = new ReplyKeyboardMarkup(new[]
                        {
                        new KeyboardButton[] { "Сохранить" }
                        })
                        {
                            ResizeKeyboard = true
                        };


                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Фото сохранено 1 из 3, хотите добавить еще фото/видео?",
                            replyMarkup: replyMediaKeyboard,
                            cancellationToken: ct);
                    }
                    else if (message.Type == MessageType.Video && message.Video != null)
                    {
                        var video = message.Video;
                        state.VideoFileId?.Add(video.FileId);
                        state.Step = 8;


                        var replyMediaKeyboard = new ReplyKeyboardMarkup(new[]
                        {
                        new KeyboardButton[] { "Сохранить" }
                        })
                        {
                            ResizeKeyboard = true
                        };


                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Видео сохранено 1 из 3, хотите добавить еще фото/видео?",
                            replyMarkup: replyMediaKeyboard,
                            cancellationToken: ct);
                    }
                    else
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Пожалуйста, отправьте фото/видео.",
                            cancellationToken: ct);
                    }
                    break;

                case 8:
                    if (message.Text?.Equals("Сохранить", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        await CreationFinalStep(
                            state.Name, state.Age, state.City,
                            state.Desc, userId, chatId,
                            state.PhotoFileId, state.VideoFileId,
                            state.Gender, state.InInterests, ct);
                    }
                    else if (message.Type == MessageType.Photo && message.Photo != null && message.Photo.Length > 0)
                    {
                        var photo = message.Photo.Last();
                        state.PhotoFileId?.Add(photo.FileId);
                        state.Step = 9;



                        var replyMediaKeyboard = new ReplyKeyboardMarkup(new[]
                        {
                        new KeyboardButton[] { "Сохранить" }
                        })
                        {
                            ResizeKeyboard = true
                        };


                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Фото сохранено 2 из 3, хотите добавить еще фото/видео?",
                            replyMarkup: replyMediaKeyboard,
                            cancellationToken: ct);
                    }
                    else if (message.Type == MessageType.Video && message.Video != null)
                    {
                        var video = message.Video;
                        state.VideoFileId?.Add(video.FileId);
                        state.Step = 9;


                        var replyMediaKeyboard = new ReplyKeyboardMarkup(new[]
                        {
                        new KeyboardButton[] { "Сохранить" }
                        })
                        {
                            ResizeKeyboard = true
                        };


                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Видео сохранено 2 из 3, хотите добавить еще фото/видео?",
                            replyMarkup: replyMediaKeyboard,
                            cancellationToken: ct);
                    }
                    else
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Пожалуйста, отправьте фото/видео.",
                            cancellationToken: ct);
                    }
                    break;

                case 9:
                    if (message.Text?.Equals("Сохранить", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        await CreationFinalStep(
                            state.Name, state.Age, state.City,
                            state.Desc, userId, chatId,
                            state.PhotoFileId, state.VideoFileId, 
                            state.Gender, state.InInterests, ct);
                    }
                    else if (message.Type == MessageType.Photo && message.Photo != null && message.Photo.Length > 0)
                    {
                        var photo = message.Photo.Last();
                        state.PhotoFileId?.Add(photo.FileId);

                        await CreationFinalStep(
                            state.Name, state.Age, state.City,
                            state.Desc, userId, chatId,
                            state.PhotoFileId, state.VideoFileId,
                            state.Gender, state.InInterests, ct);
                    }
                    else if (message.Type == MessageType.Video && message.Video != null)
                    {
                        var video = message.Video;
                        state.VideoFileId?.Add(video.FileId);

                        await CreationFinalStep(
                            state.Name, state.Age, state.City,
                            state.Desc, userId, chatId,
                            state.PhotoFileId, state.VideoFileId,
                            state.Gender, state.InInterests, ct);
                    }

                    break;

                default:
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Некорректный ввод, попробуйте снова",
                        cancellationToken: ct);
                    break;
            }
        }

        private async Task CreationFinalStep(
            string? name,
            int age,
            string? city,
            string? desc,
            long userId,
            long chatId,
            List<string>? pfileIds,
            List<string>? vfileIds,
            Gender? gender,
            Gender? inInterests,
            CancellationToken ct)
        {
            var command = new Profile
            {
                Name = name,
                Age = age,
                City = city,
                UserId = userId,
                ChatId = chatId,
                Bio = desc,
                Gender = gender,
                InInterests = inInterests,
                Photos = new List<Photo>(),
                Videos = new List<Video>()
            };

            if (pfileIds != null)
            {
                foreach(var fileId in pfileIds)
                {
                    command.Photos.Add(new Photo { FileId = fileId });
                }
            }

            if (vfileIds != null)
            {
                foreach (var fileId in vfileIds)
                {
                    command.Videos.Add(new Video { FileId = fileId });
                }
            }

            var response = await _httpClient.PostAsJsonAsync("/api/profile", command, ct);
            if (response.IsSuccessStatusCode)
            {
                var removeKeyboard = new ReplyKeyboardRemove();

                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Ваша анкета успешно создана!",
                    replyMarkup: removeKeyboard,
                    cancellationToken: ct);

                var replyMediaKeyboard = new ReplyKeyboardMarkup(new[]
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
                chatId: chatId,
                text: "Начинайте искать людей, удачи!",
                replyMarkup: replyMediaKeyboard,
                cancellationToken: ct);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[API ERROR] StatusCode: {(int)response.StatusCode}, Content: {errorContent}");
                
                var removeKeyboard = new ReplyKeyboardRemove();
                
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "❌ Ошибка при добавлении",
                    replyMarkup: removeKeyboard,
                    cancellationToken: ct);
            }

            _state.Remove(chatId);
        }


        //--------------------------------------CHECK MY PROFILE------------------------------------------


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
                    new KeyboardButton("📝 Заполнить анкету заново")
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

        //--------------------------------------DEFAULT------------------------------------------

        public async Task HandleDefaultCommand(long chatId, CancellationToken ct)
        {
            await _botClient.SendMessage(
                chatId: chatId,
                text: "Не понимаю, о чем ты😅",
                cancellationToken: ct);
        }


        //--------------------------------------ERROR------------------------------------------

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return Task.CompletedTask;
        }


    }
}