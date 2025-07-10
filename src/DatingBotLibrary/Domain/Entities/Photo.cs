using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatingBotLibrary.Domain.Entities
{
    public class Photo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? FileId { get; set; }
        public string? ProfileId { get; set; }
        public Profile? UserProfile { get; set; }
    }
}
