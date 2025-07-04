using DatingBotLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatingBotLibrary.Domain.Interfaces
{
    public interface IProfileRepository
    {
        Task<Profile> CheckMyProfile(long chatId);
        Task CreateProfile(Profile profile);

        Task<IEnumerable<Profile>> GetAllProfiles();
        
        Task DeleteProfileLike(long myId, long likeId);
        Task UpdateProfileForLike(long myId, long likeId);
    }
}
