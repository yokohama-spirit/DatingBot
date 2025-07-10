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
        private readonly ILikesRepository _rep;
        private readonly IProfilesSearchRepository _search;

        public LikesController
            (ILikesRepository rep,
            IProfilesSearchRepository search)
        {
            _rep = rep;
            _search = search;
        }


        // Method to get all users who liked the profile of the current user
        [HttpGet("s/{chatId}")]
        public async Task<IActionResult> GetLikesProfiles(long chatId)
        {
            try
            {
                var profiles = await _search.GetLikesProfiles(chatId);

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


        // Method to change the list of likes of the current user (replenishes the list of chatId)
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


        // Method to remove user like (clear likes after liked user has already viewed likes of his profile)
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


        // Method for getting the number of likes that are hanging on a user's profile (for variety of messages)
        [HttpGet("count/{chatId}")]
        public async Task<ActionResult<decimal>> GetLikesCount(long chatId)
        {
            try
            {
                var count = await _search.CountChecker(chatId);

                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting matching profiles: {ex}");
                return Ok(Array.Empty<Profile>());
            }
        }

    }
}
