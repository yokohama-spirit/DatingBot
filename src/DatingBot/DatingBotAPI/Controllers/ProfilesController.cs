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

        public ProfilesController
            (IProfileRepository rep,
            IProfilesSearchRepository search,
            DatabaseConnect conn)
        {
            _rep = rep;
            _search = search;
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


        [HttpGet("unfrozen/{chatId}")]
        public async Task<ActionResult<bool>> MakeMeUnfrozen(long chatId)
        {
            var result = await _rep.MakeMeUnfrozen(chatId);
            return Ok(result);
        }

        [HttpGet("frozen/{chatId}")]
        public async Task<ActionResult<bool>> MakeMeFrozen(long chatId)
        {
            var result = await _rep.MakeMeFrozen(chatId);
            return Ok(result);
        }


        [HttpPost("set")]
        public async Task<IActionResult> SetMany()
        {
            await _rep.SetPeople();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> All()
        {
            var result = await _rep.GetAllProfiles();
            return Ok(result);
        }
    }
}
