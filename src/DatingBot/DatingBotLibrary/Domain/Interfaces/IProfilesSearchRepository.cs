using DatingBotLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatingBotLibrary.Domain.Interfaces
{
    public interface IProfilesSearchRepository
    {
        Task<List<Profile>> GetProfiles(long chatId);

        Task<List<Profile>> GetLikesProfiles(long chatId);
    }
}
