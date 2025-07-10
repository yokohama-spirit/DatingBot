using DatingBotLibrary.Domain.Entities;
using Telegram.Bot;
using TelegramBot.Config;
using TelegramBot.Config.State;
using TelegramBot.Interfaces;
using TelegramBot.Interfaces.Other;
using TelegramBot.Services;
using TelegramBot.Services.Other;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//config
var botConfig = builder.Configuration.GetSection("TelegramBotConfig").Get<TelegramBotConfig>();
builder.Services.AddSingleton(botConfig);

//HTTP-client
builder.Services.AddHttpClient("BotApi", client =>
{
    client.BaseAddress = new Uri(botConfig.ApiBaseUrl);
});


builder.Services.AddSingleton<ITelegramBotClient>(_ =>
    new TelegramBotClient(botConfig.Token));



//DI
builder.Services.AddSingleton<IFrozenService, FrozenService>();
builder.Services.AddSingleton<IStartService, StartService>();
builder.Services.AddSingleton<ILikesService, LikesService>();
builder.Services.AddSingleton<IProfilesService, ProfilesService>();
builder.Services.AddSingleton<IHandleStartCommand, HandleStartCommand>();
builder.Services.AddSingleton<ICreateProfileService, CreateProfileService>();
builder.Services.AddSingleton<ITelegramBotService, TelegramBotService>();
//-----------------------------DICTIONARIES------------------------------
builder.Services.AddKeyedSingleton<Dictionary<long, CreateProfileState>>("state");
builder.Services.AddKeyedSingleton<Dictionary<long, List<Profile>>>("datingProfiles");
builder.Services.AddKeyedSingleton<Dictionary<long, List<Profile>>>("checkLikes");
builder.Services.AddKeyedSingleton<Dictionary<long, Profile>>("likes");
builder.Services.AddKeyedSingleton<Dictionary<long, Profile>>("mutually");


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// bot start
var botService = app.Services.GetRequiredService<ITelegramBotService>();
var cts = new CancellationTokenSource();
await botService.StartAsync(cts.Token);

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();