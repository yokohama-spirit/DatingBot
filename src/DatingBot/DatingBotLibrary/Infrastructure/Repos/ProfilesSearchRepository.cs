using DatingBotLibrary.Domain.Entities;
using DatingBotLibrary.Domain.Interfaces;
using DatingBotLibrary.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DatingBotLibrary.Infrastructure.Repos
{
    public class ProfilesSearchRepository : IProfilesSearchRepository
    {
        private readonly DatabaseConnect _conn;

        public ProfilesSearchRepository
            (DatabaseConnect conn)
        {
            _conn = conn;
        }

        public async Task<List<Profile>> GetLikesProfiles(long chatId)
        {
            var userProfile = await _conn.Profiles
                .FirstOrDefaultAsync(p => p.ChatId == chatId);

            if (userProfile == null)
                return new List<Profile>();


            var profiles = await _conn.Profiles
                .Where(p => p.ChatId != chatId)
                .Where(p => p.Likes.Contains(chatId))
                .OrderBy(_ => Guid.NewGuid())
                .Include(p => p.Photos)
                .Include(p => p.Videos)
                .ToListAsync();

            return profiles;
        }

        public async Task<List<Profile>> GetProfiles(long chatId)
        {
            var userProfile = await _conn.Profiles
                .FirstOrDefaultAsync(p => p.ChatId == chatId);

            if (userProfile == null)
                return new List<Profile>();


            var profiles = await _conn.Profiles
                .Where(p => p.ChatId != chatId)
                .Where(p => p.isFrozen == false)
                .Where(p => p.Gender == userProfile.InInterests)
                .Where(p => Math.Abs(p.Age - userProfile.Age) <= 2)
                .Where(p => p.City != null && userProfile.City != null &&
                    p.City.Replace(" ", "").ToLower() == userProfile.City.Replace(" ", "").ToLower())
                .OrderBy(_ => Guid.NewGuid())
                .Include(p => p.Photos)
                .Include(p => p.Videos)
                .ToListAsync();

            return profiles;
        }
    }
}
