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
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DatingBotLibrary.Infrastructure.Repos
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly DatabaseConnect _conn;
        private readonly IDistributedCache _redisCache;

        public ProfileRepository
            (DatabaseConnect conn,
            IDistributedCache redisCache)
        {
            _conn = conn;
            _redisCache = redisCache;
        }

        public async Task<Profile> CheckMyProfile(long chatId)
        {
            var cacheKey = $"my_profile:{chatId}";

            // Настройки сериализации (добавьте в класс или используйте глобальные)
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve,
                WriteIndented = false
            };

            // Проверяем кэш
            var cachedData = await _redisCache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                try
                {
                    return JsonSerializer.Deserialize<Profile>(cachedData, serializerOptions);
                }
                catch (JsonException)
                {
                    // Если данные в кэше повреждены - игнорируем и загружаем заново
                    await _redisCache.RemoveAsync(cacheKey);
                }
            }

            // Получаем из базы
            var profile = await _conn.Profiles
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(p => p.ChatId == chatId)
                ?? throw new Exception("Профиль не найден");

            // Кэшируем с настройками
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            };

            await _redisCache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(profile, serializerOptions),
                cacheOptions);

            return profile;
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
