using DatingBotLibrary.Domain.Entities;
using DatingBotLibrary.Domain.Entities.Enum;
using Microsoft.AspNetCore.Http.HttpResults;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Config;
using TelegramBot.Config.State;
using TelegramBot.Interfaces;
using TelegramBot.Interfaces.Other;

namespace TelegramBot.Services
{
    public class CreateProfileService : ICreateProfileService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly TelegramBotConfig _config;
        private readonly HttpClient _httpClient;
        private readonly IFrozenService _frozen;
        private readonly IStartService _start;
        private readonly ILikesService _likesService;
        private readonly IProfilesService _profile;
        private readonly IHandleStartCommand _command;
        private readonly Dictionary<long, CreateProfileState> _state;
        private readonly Dictionary<long, List<Profile>> _datingProfiles;
        private readonly Dictionary<long, Profile> _likes;
        private readonly Dictionary<long, Profile> _mutually;
        private readonly Dictionary<long, List<Profile>> _checkLikes;


        public CreateProfileService
            (TelegramBotConfig config,
            IFrozenService frozen,
            IStartService start,
            ILikesService likesService,
            IHttpClientFactory httpClientFactory,
            IProfilesService profile,
            IHandleStartCommand command,
            [FromKeyedServices("state")] Dictionary<long, CreateProfileState> state,
            [FromKeyedServices("likes")] Dictionary<long, Profile> likes,
            [FromKeyedServices("mutually")] Dictionary<long, Profile> mutually,
            [FromKeyedServices("checkLikes")] Dictionary<long, List<Profile>> checkLikes,
            [FromKeyedServices("checkLikes")] Dictionary<long, List<Profile>> datingProfiles)
        {
            _config = config;
            _botClient = new TelegramBotClient(_config.Token);
            _httpClient = httpClientFactory.CreateClient("BotApi");
            _frozen = frozen;
            _profile = profile;
            _start = start;
            _state = state;
            _datingProfiles = datingProfiles;
            _likes = likes;
            _mutually = mutually;
            _checkLikes = checkLikes;
            _likesService = likesService;
            _command = command;
        }



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
            await StateCleaner(message.Text, chatId, ct);

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
                    new[] 
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
                        new[] 
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
                        new[] 
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
                Videos = new List<DatingBotLibrary.Domain.Entities.Video>()
            };

            if (pfileIds != null)
            {
                foreach (var fileId in pfileIds)
                {
                    command.Photos.Add(new Photo { FileId = fileId });
                }
            }

            if (vfileIds != null)
            {
                foreach (var fileId in vfileIds)
                {
                    command.Videos.Add(new DatingBotLibrary.Domain.Entities.Video { FileId = fileId });
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

        private async Task StateCleaner(string text, long chatId, CancellationToken ct)
        {
            bool textIsCommand = text == "/start"
                             || text == "Главное меню"
                             || text == "☃️ Создать анкету"
                             || text == "📝 Заполнить анкету заново"
                             || text == "👤 Моя анкета"
                             || text == "🚀 Смотреть анкеты"
                             || text == "❤️ Лайк"
                             || text == "👎 Дизлайк"
                             || text == "Посмотреть"
                             || text == "❤️ Взаимно"
                             || text == "💔 Невзаимно"
                             || text == "Разморозить анкету 😴"
                             || text == "💤"
                             || text == "🚫 Прекратить просмотр";

            if (textIsCommand)
            {
                await ClearAllStates(chatId, ct);


                switch (text)
                {
                    case "/start":
                    case "Главное меню":
                        if (await _start.ProfileChecker(chatId, ct))
                        {
                            await _profile.SendUserProfile(chatId, ct);
                        }
                        else
                        {
                            await _command.StartCommand(chatId, ct);
                        }
                        break;

                    case "☃️ Создать анкету":
                        await HandleCreateCommand(chatId, ct);
                        break;

                    case "📝 Заполнить анкету заново":
                        await HandleCreateCommand(chatId, ct);
                        break;

                    case "👤 Моя анкета":
                        await _profile.SendUserProfile(chatId, ct);
                        break;

                    case "🚀 Смотреть анкеты":
                        await _profile.StartProfileViewing(chatId, ct);
                        break;

                    case "❤️ Лайк":
                        await _likesService.LikesHandler(chatId, ct);
                        break;

                    case "👎 Дизлайк":
                        await _profile.ShowRandomProfile(chatId, ct);
                        break;

                    case "Посмотреть":
                        await _likesService.StartLikesViewing(chatId, ct);
                        break;

                    case "Неинтересно":

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
                        text: "Выберите действие:",
                        replyMarkup: replyKeyboard,
                        cancellationToken: ct);
                        break;


                    case "❤️ Взаимно":
                        await _likesService.MutuallyHandler(chatId, ct);
                        await _likesService.ShowLikesProfiles(chatId, ct);
                        break;

                    case "💔 Невзаимно":
                        await _likesService.NonMutuallyHandler(chatId, ct);
                        break;

                    case "Разморозить анкету 😴":
                        await _frozen.UnfrozenHandle(chatId, ct);
                        break;

                    case "💤":
                        await _frozen.FrozenHandle(chatId, ct);
                        break;

                    case "🚫 Прекратить просмотр":

                        var replyStopKeyboard = new ReplyKeyboardMarkup(new[]
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
                            text: "Просмотр анкет завершен",
                            replyMarkup: new ReplyKeyboardRemove(),
                            cancellationToken: ct);

                        await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Выберите действие:",
                        replyMarkup: replyStopKeyboard,
                        cancellationToken: ct);
                        break;
                }
            }
        }

        public async Task ClearAllStates(long chatId, CancellationToken ct)
        {
            _state.Remove(chatId);
            _datingProfiles.Remove(chatId);
            _checkLikes.Remove(chatId);
            _likes.Remove(chatId);
            _mutually.Remove(chatId);
        }
    }
}
