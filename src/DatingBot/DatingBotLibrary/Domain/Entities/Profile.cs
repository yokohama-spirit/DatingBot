using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatingBotLibrary.Domain.Entities
{
    public class Profile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public long UserId { get; set; }
        public long ChatId { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? City { get; set; }
        public string? Bio { get; set; }
        public List<Photo> Photos { get; set; } = new();
        public List<Video> Videos { get; set; } = new();
    }
}
