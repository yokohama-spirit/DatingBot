using DatingBotLibrary.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatingBotLibrary.Application.Requests
{
    public class CreateProfileRequest : IRequest
    {
        public long UserId { get; set; }
        public long ChatId { get; set; }
        public required string Name { get; set; }
        public required int Age { get; set; }
        public required string City { get; set; }
        public string? Bio { get; set; }
        public List<Photo> Photos { get; set; } = new();
    }
}
