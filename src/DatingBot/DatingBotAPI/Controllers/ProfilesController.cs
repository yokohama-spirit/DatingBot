using DatingBotLibrary.Domain.Entities;
using DatingBotLibrary.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DatingBotLibrary.Infrastructure.Data;

namespace DatingBotAPI.Controllers
{
    [Route("api/profile")]
    [ApiController]
    public class ProfilesController : ControllerBase
    {
        private readonly IProfileRepository _rep;
        private readonly IProfilesSearchRepository _search;
        private readonly DatabaseConnect _context;

        public ProfilesController
            (IProfileRepository rep,
            IProfilesSearchRepository search,
            DatabaseConnect conn)
        {
            _rep = rep;
            _search = search;
            _context = conn;
        }



        [HttpPost]
        public async Task<IActionResult> CreateProfile([FromBody] Profile command)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(e => e.Errors.Select(er => er.ErrorMessage));
                return BadRequest($"Некорректно указаны данные! Ошибка: {error}");
            }
            try
            {
                await _rep.CreateProfile(command);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }


        [HttpGet("{chatId}")]
        public async Task<ActionResult<Profile>> CheckMyProfile(long chatId)
        {
            try
            {
                var result = await _rep.CheckMyProfile(chatId);
                return result;
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }



        [HttpGet("s/{chatId}")]
        public async Task<IActionResult> GetMatchingProfiles(long chatId)
        {
            try
            {
                // 1. Получаем профиль пользователя
                var userProfile = await _context.Profiles
                    .FirstOrDefaultAsync(p => p.ChatId == chatId);

                if (userProfile == null)
                    return Ok(Array.Empty<Profile>());


                var profiles = await _context.Profiles
                    .Where(p => p.ChatId != chatId) 
                    .Where(p => p.Gender == userProfile.InInterests) 
                    .Where(p => Math.Abs(p.Age - userProfile.Age) <= 2)
                    .Where(p => p.City != null && userProfile.City != null &&
                        p.City.Replace(" ", "").ToLower() == userProfile.City.Replace(" ", "").ToLower())
                    .OrderBy(_ => Guid.NewGuid())
                    .Include(p => p.Photos)
                    .Include(p => p.Videos)
                    .ToListAsync();



                foreach (var p in profiles)
                {
                    Console.WriteLine($"Match: {p.Name}, {p.Age}, {p.City}, Gender: {p.Gender}, Interests: {p.InInterests}");
                }

                return new JsonResult(profiles, new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting matching profiles: {ex}");
                return Ok(Array.Empty<Profile>());
            }
        }
        [HttpGet]
        public async Task<IActionResult> All()
        {
            var result = await _rep.GetAllProfiles();
            return Ok(result);
        }
    }
}
