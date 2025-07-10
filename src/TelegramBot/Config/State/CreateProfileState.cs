using DatingBotLibrary.Domain.Entities.Enum;

namespace TelegramBot.Config.State
{
    public class CreateProfileState
    {
        public int Step { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? City { get; set; }
        public string? Desc { get; set; }
        public Gender? Gender { get; set; }
        public Gender? InInterests { get; set; }
        public List<string>? PhotoFileId { get; set; }
        public List<string>? VideoFileId { get; set; }
    }
}
