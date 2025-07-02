using AutoMapper;
using DatingBotLibrary.Application.Requests;
using DatingBotLibrary.Infrastructure.Data;
using MediatR;
using Profile = DatingBotLibrary.Domain.Entities.Profile;

namespace DatingBotLibrary.Application.Services
{
    public class CreateProfileRequestHandle : IRequestHandler<CreateProfileRequest>
    {
        private readonly DatabaseConnect _conn;
        private readonly IMapper _mapper;

        public CreateProfileRequestHandle
            (DatabaseConnect conn,
            IMapper mapper)
        {
            _conn = conn;
            _mapper = mapper;
        }

        public async Task Handle(CreateProfileRequest request, CancellationToken cancellationToken)
        {
            var profile = _mapper.Map<Profile>(request);

            await _conn.Profiles.AddAsync(profile);
            await _conn.SaveChangesAsync();
        }
    }
}
