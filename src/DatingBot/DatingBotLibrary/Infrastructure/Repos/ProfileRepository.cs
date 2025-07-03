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
/*            var cacheKey = $"my_profile:{chatId}";


            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve,
                WriteIndented = false
            };


            var cachedData = await _redisCache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                try
                {
                    return JsonSerializer.Deserialize<Profile>(cachedData, serializerOptions);
                }
                catch (JsonException)
                {
                    await _redisCache.RemoveAsync(cacheKey);
                }
            }*/


            var profile = await _conn.Profiles
                .Include(p => p.Photos)
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.ChatId == chatId)
                ?? throw new Exception("Профиль не найден");


/*            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            };

            await _redisCache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(profile, serializerOptions),
                cacheOptions);*/

            return profile;
        }

        public async Task CreateProfile(Profile profile)
        {
            var existingProfile = await _conn.Profiles
                .Include(p => p.Photos)
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.ChatId == profile.ChatId);

            if (existingProfile != null)
            {

                existingProfile.Name = profile.Name;
                existingProfile.Age = profile.Age;
                existingProfile.City = profile.City;
                existingProfile.Bio = profile.Bio;


                existingProfile.Photos.Clear();
                existingProfile.Videos.Clear();


                foreach (var photo in profile.Photos)
                {
                    existingProfile.Photos.Add(photo);
                }

                foreach (var video in profile.Videos)
                {
                    existingProfile.Videos.Add(video);
                }
            }
            else
            {
                await _conn.Profiles.AddAsync(profile);
            }

            await _conn.SaveChangesAsync();
        }

        public async Task<IEnumerable<Profile>> GetAllProfiles()
        {
            return await _conn.Profiles
                .Include(p => p.Photos)
                .Include(p => p.Videos)
                .ToListAsync();
        }
    }
}
