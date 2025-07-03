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

namespace TelegramBot.Services
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly TelegramBotConfig _config;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<long, CreateProfileState> _state;

        public TelegramBotService
            (TelegramBotConfig config)
        {
            _config = config;
            _botClient = new TelegramBotClient(_config.Token);
            _httpClient = new HttpClient { BaseAddress = new Uri(_config.ApiBaseUrl) };
            _state = new Dictionary<long, CreateProfileState>();
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
            if (update.Message is not { } message)
                return;

            long chatId = message.Chat.Id;


            if (_state.TryGetValue(chatId, out var state))
            {
                await HandleCreateInputCommand(chatId, update.Message, ct);
                return;
            }

            if (message.Text is not { } text)
                return;


            switch (text)
            {
                case "/start":
                case "Главное меню":
                    await HandleStartCommand(chatId, ct);
                    break;

                case "☃️ Создать анкету":
                    await HandleCreateCommand(chatId, ct);
                    break;

                case "👤 Моя анкета":
                    await SendUserProfile(chatId, ct);
                    break;

                case "Тест":
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Тестовая кнопка работает!",
                        cancellationToken: ct);
                    break;

                default:
                    await HandleDefaultCommand(chatId, ct);
                    break;
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


                    await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Отправьте фотографию/видео для своей анкеты.",
                    cancellationToken: ct);

                    state.Step = 5;

                    break;

                case 5:
                    if (message.Type == MessageType.Photo && message.Photo != null && message.Photo.Length > 0)
                    {
                        var photo = message.Photo.Last();
                        state.PhotoFileId?.Add(photo.FileId);
                        state.Step = 6;



                        var replyMediaKeyboard = new ReplyKeyboardMarkup(new[]
                        {
                        new KeyboardButton[] { "Пропустить" }
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
                        state.Step = 6;


                        var replyMediaKeyboard = new ReplyKeyboardMarkup(new[]
                        {
                        new KeyboardButton[] { "Пропустить" }
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

                case 6:
                    if (message.Text?.Equals("Пропустить", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        await CreationFinalStep(
                            state.Name, state.Age, state.City,
                            state.Desc, userId, chatId,
                            state.PhotoFileId, state.VideoFileId, ct);
                    }
                    else if (message.Type == MessageType.Photo && message.Photo != null && message.Photo.Length > 0)
                    {
                        var photo = message.Photo.Last();
                        state.PhotoFileId?.Add(photo.FileId);
                        state.Step = 7;



                        var replyMediaKeyboard = new ReplyKeyboardMarkup(new[]
                        {
                        new KeyboardButton[] { "Пропустить" }
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
                        state.Step = 7;


                        var replyMediaKeyboard = new ReplyKeyboardMarkup(new[]
                        {
                        new KeyboardButton[] { "Пропустить" }
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
                case 7:
                    if (message.Text?.Equals("Пропустить", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        await CreationFinalStep(
                            state.Name, state.Age, state.City,
                            state.Desc, userId, chatId,
                            state.PhotoFileId, state.VideoFileId, ct);
                    }
                    else if (message.Type == MessageType.Photo && message.Photo != null && message.Photo.Length > 0)
                    {
                        var photo = message.Photo.Last();
                        state.PhotoFileId?.Add(photo.FileId);

                        await CreationFinalStep(
                        state.Name, state.Age, state.City,
                        state.Desc, userId, chatId,
                        state.PhotoFileId, state.VideoFileId, ct);
                    }
                    else if (message.Type == MessageType.Video && message.Video != null)
                    {
                        var video = message.Video;
                        state.VideoFileId?.Add(video.FileId);

                        await CreationFinalStep(
                        state.Name, state.Age, state.City,
                        state.Desc, userId, chatId,
                        state.PhotoFileId, state.VideoFileId, ct);
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
                new[] //сверху
                {
                new KeyboardButton("🚀 Смотреть анкеты"),
                new KeyboardButton("👤 Моя анкета"),
                new KeyboardButton("Тест")
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