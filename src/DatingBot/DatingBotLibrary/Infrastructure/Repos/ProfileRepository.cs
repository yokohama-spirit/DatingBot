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

        public ProfileRepository
            (DatabaseConnect conn)
        {
            _conn = conn;
        }

        public async Task<Profile> CheckMyProfile(long chatId)
        {
            return await _conn.Profiles
                .Include(p => p.Photos)
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.ChatId == chatId)
                ?? throw new Exception("Профиль не найден");
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
