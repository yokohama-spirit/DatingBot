using DatingBotLibrary.Domain.Entities;
using DatingBotLibrary.Domain.Interfaces;
using DatingBotLibrary.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace DatingBotAPI.Controllers
{
    [Route("api/likes")]
    [ApiController]
    public class LikesController : ControllerBase
    {
        private readonly IProfileRepository _rep;
        private readonly IProfilesSearchRepository _search;

        public LikesController
            (IProfileRepository rep,
            IProfilesSearchRepository search)
        {
            _rep = rep;
            _search = search;
        }


        [HttpGet("s/{chatId}")]
        public async Task<IActionResult> GetLikesProfiles(long chatId)
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


        [HttpPut("u/{myId}/{likeId}")]
        public async Task<IActionResult> UpdateLikes(long myId, long likeId)
        {
            try
            {
                await _rep.UpdateProfileForLike(myId, likeId);
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting matching profiles: {ex}");
                return Ok(Array.Empty<Profile>());
            }
        }


        [HttpPut("d/{myId}/{likeId}")]
        public async Task<IActionResult> DeleteLike(long myId, long likeId)
        {
            try
            {
                await _rep.DeleteProfileLike(myId, likeId);
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting matching profiles: {ex}");
                return Ok(Array.Empty<Profile>());
            }
        }

    }
}
