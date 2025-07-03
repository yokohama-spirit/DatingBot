using DatingBotLibrary.Domain.Entities;
using DatingBotLibrary.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatingBotAPI.Controllers
{
    [Route("api/profile")]
    [ApiController]
    public class ProfilesController : ControllerBase
    {
        private readonly IProfileRepository _rep;
        public ProfilesController
            (IProfileRepository rep)
        {
            _rep = rep;
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

        [HttpGet]
        public async Task<IActionResult> All()
        {
            var result = await _rep.GetAllProfiles();
            return Ok(result);
        }
    }
}
