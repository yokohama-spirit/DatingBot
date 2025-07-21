using DatingBotLibrary.Domain.Entities;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Config;
using TelegramBot.Config.State;
using TelegramBot.Interfaces;
using TelegramBot.Interfaces.Other;

namespace TelegramBot.Services
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly TelegramBotConfig _config;
        private readonly IFrozenService _frozen;
        private readonly IStartService _start;
        private readonly ILikesService _likesService;
        private readonly ICreateProfileService _create;
        private readonly IProfilesService _profile;
        private readonly IHandleStartCommand _command;
        private readonly Dictionary<long, CreateProfileState> _state;


        public TelegramBotService
            (TelegramBotConfig config,
            IFrozenService frozen,
            IStartService start,
            ILikesService likesService,
            ICreateProfileService create,
            IHandleStartCommand command,
            IProfilesService profile,
            [FromKeyedServices("state")] Dictionary<long, CreateProfileState> state)
        {
            _config = config;
            _botClient = new TelegramBotClient(_config.Token);
            _frozen = frozen;
            _profile = profile;
            _start = start;
            _state = state;
            _create = create;
            _likesService = likesService;
            _command = command;
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
                        await _create.HandleCreateInputCommand(chatId, message, ct);
                        return;
                    }

                    // Checking the existence of a profile

                    var response = await _start.ProfileChecker(chatId, ct);


                    if (response == false && message.Text == "☃️ Создать анкету")
                    {
                        await _create.HandleCreateCommand(chatId, ct);
                    }
                    else if (response == false)
                    {
                        await _command.StartCommand(chatId, ct);
                    }

                    //-------------------------------------------------------------------------

                    var isFrozen = await _frozen.IsFrozen(chatId, ct);

                    if (isFrozen && message.Text != "Разморозить анкету 😴")
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
                            text: "Для начала разморозьте свою анкету!",
                            replyMarkup: replyKeyboard,
                            cancellationToken: ct);
                        return;
                    }
                    else if (isFrozen && message.Text == "Разморозить анкету 😴")
                    {
                        await _frozen.UnfrozenHandle(chatId, ct);
                        return;
                    }


                    switch (message.Text)
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
                        case "📝 Заполнить анкету заново":
                            await _create.HandleCreateCommand(chatId, ct);
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
                            await _create.ClearAllStates(chatId, ct);

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