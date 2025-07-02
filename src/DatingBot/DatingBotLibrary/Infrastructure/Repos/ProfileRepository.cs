using Microsoft.EntityFrameworkCore;
using DatingBotLibrary.Domain.Entities;
using DatingBotLibrary.Domain.Interfaces;
using DatingBotLibrary.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Profile = DatingBotLibrary.Domain.Entities.Profile;

namespace DatingBotLibrary.Infrastructure.Repos
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly DatabaseConnect _conn;


        public ProfileRepository
            (DatabaseConnect conn)
        {
            _conn = conn;
        }

        public async Task<Profile> CheckMyProfile(long chatId)
        {
            return await _conn.Profiles
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(p => p.ChatId == chatId)
                ?? throw new Exception("Профиль не найден");

        }

        public async Task CreateProfile(Profile profile)
        {
            await _conn.Profiles.AddAsync(profile);
            await _conn.SaveChangesAsync();
        }

        public async Task<IEnumerable<Profile>> GetAllProfiles()
        {
            return await _conn.Profiles.Include(p => p.Photos).ToListAsync();
        }
    }
}
