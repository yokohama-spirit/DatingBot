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
        private readonly IStartRepository _start;

        public ProfilesController
            (IProfileRepository rep,
            IProfilesSearchRepository search,
            DatabaseConnect conn,
            IStartRepository start)
        {
            _rep = rep;
            _search = search;
            _start = start;
        }


        // Method for creating a profile
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


        // Method for getting your own profile
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


        // Method for obtaining user-appropriate profiles
        [HttpGet("s/{chatId}")]
        public async Task<IActionResult> GetMatchingProfiles(long chatId)
        {
            try
            {
                var profiles = await _search.GetProfiles(chatId);

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


        // Method for defrosting a profile
        [HttpGet("unfrozen/{chatId}")]
        public async Task<ActionResult<bool>> MakeMeUnfrozen(long chatId)
        {
            var result = await _rep.MakeMeUnfrozen(chatId);
            return Ok(result);
        }


        // Method for freezing a profile
        [HttpGet("frozen/{chatId}")]
        public async Task<ActionResult<bool>> MakeMeFrozen(long chatId)
        {
            var result = await _rep.MakeMeFrozen(chatId);
            return Ok(result);
        }


        // Method with checking the existence of the current user profile
        [HttpGet("ise/{chatId}")]
        public async Task<ActionResult<bool>> isExists(long chatId)
        {
            try
            {
                var result = await _start.isExistsProfile(chatId);
                return result;
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }
    }
}
