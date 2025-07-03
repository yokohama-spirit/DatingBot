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

        public async Task<List<Profile>> GetProfiles(long chatId)
        {
            // Получаем профиль пользователя
            var userProfile = await _conn.Profiles
                .FirstOrDefaultAsync(p => p.ChatId == chatId);

            if (userProfile == null)
                return new List<Profile>();

            // Получаем все профили (кроме своего) в память
            var allProfiles = await _conn.Profiles
                .Where(p => p.ChatId != chatId)
                .Include(p => p.Photos)
                .Include(p => p.Videos)
                .AsNoTracking()
                .ToListAsync();

            // Фильтруем в памяти
            var normalizedCity = userProfile.City?
                .Replace(" ", "")
                .Trim()
                .ToLowerInvariant();

            var matchingProfiles = allProfiles
                .Where(p => p.Gender == userProfile.InInterests)
                .Where(p => Math.Abs(p.Age - userProfile.Age) <= 2)
                .Where(p =>
                    string.IsNullOrEmpty(normalizedCity) ||
                    (p.City != null &&
                     p.City.Replace(" ", "").Trim().ToLowerInvariant() == normalizedCity))
                .OrderBy(_ => Guid.NewGuid()) // Рандомный порядок
                .ToList();

            return matchingProfiles;
        }
    }
}
