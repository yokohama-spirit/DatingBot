using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using TelegramBot.Interfaces;
using TelegramBot.Config;
using TelegramBot.Config.State;
using Telegram.Bot.Types.ReplyMarkups;
using DatingBotLibrary.Application.Requests;
using DatingBotLibrary.Domain.Entities;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Text.Json.Serialization;
using System.Text.Json;

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

            if (message.Text is { } text)
            {
                switch (text)
                {
                    case "/start":
                        await HandleStartCommand(chatId, ct);
                        break;

                    case "/create":
                        await HandleCreateCommand(chatId, ct);
                        break;

                    case "/check":
                        await SendUserProfile(chatId, ct);
                        break;

                    default:
                        await HandleDefaultCommand(chatId, ct);
                        break;
                }
            }

        }

        //--------------------------------------START COMMAND------------------------------------------

        public async Task HandleStartCommand(long chatId, CancellationToken ct)
        {
            var chat = await _botClient.GetChat(chatId, ct);

            await _botClient.SendMessage(
                chatId: chatId,
                text: $"Привет, {chat.FirstName ?? "друг"}! Я - бот для знакомств!\n" +
                $"/create - создание анкеты\n" +
                $"/check - просмотр своей анкеты",
                cancellationToken: ct);
        }

        //--------------------------------------CREATE------------------------------------------

        public async Task HandleCreateCommand(long chatId, CancellationToken ct)
        {
            _state[chatId] = new CreateProfileState
            {
                Step = 1
            };

            await _botClient.SendMessage(
                chatId: chatId,
                text: "Как вас зовут?",
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
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
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
                    text: "Отправьте фотографию для своей анкеты.",
                    cancellationToken: ct);

                    state.Step = 5;

                    break;

                case 5:

                    if (message.Type != MessageType.Photo || message.Photo == null || message.Photo.Length == 0)
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Пожалуйста, отправьте именно фотографию (не текст и не стикер).",
                            cancellationToken: ct);
                        return;
                    }

                    var photo = message.Photo.Last();
                    Console.WriteLine($"Received message type: {message.Type}");

                    await CreationFinalStep
                        (state.Name, state.Age, state.City,
                        state.Desc, userId, chatId, photo.FileId, ct);

                    break;

                default:
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Некорректный ввод, попробуйте снова",
                        cancellationToken: ct);
                    break;
            }
        }

        private async Task CreationFinalStep
            (string name, 
            int age,
            string city,
            string desc,
            long userId,
            long chatId,
            string fileId,
            CancellationToken ct)
        {
            var command = new Profile
            {
                Name = name,
                Age = age,
                City = city,
                UserId = userId,
                ChatId = chatId,
                Bio = desc
            };

            command.Photos.Add(new Photo
            {
                FileId = fileId
            });


            var response = await _httpClient.PostAsJsonAsync("/api/profile", command, ct);
            if (response.IsSuccessStatusCode)
            {
                await _botClient.SendMessage(
                chatId: chatId,
                text: "Ваша анкета успешно создана!",
                cancellationToken: ct);
            }
            else
            {

                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[API ERROR] StatusCode: {(int)response.StatusCode}, Content: {errorContent}");


                await _botClient.SendMessage(
                chatId: chatId,
                text: "❌ Ошибка при добавлении",
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


                var caption = $"{profile.Name}, {profile.Age}, {profile.City} – {profile.Bio ?? "Без описания"}";


                var photos = profile.Photos;

                if (photos.Count > 0)
                {
                    var mediaGroup = photos
                        .Select((photo, index) =>
                        {
                            var media = new InputMediaPhoto(new InputFileId(photo.FileId));
                            if (index == 0) media.Caption = caption;
                            return media;
                        })
                        .ToList();

                    await _botClient.SendMediaGroup(
                        chatId: chatId,
                        media: mediaGroup,
                        cancellationToken: ct);
                }
                else
                {
                    await _botClient.SendMessage(chatId, caption, cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex}");
                await _botClient.SendMessage(chatId, "⚠️ Ошибка при загрузке профиля", cancellationToken: ct);
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