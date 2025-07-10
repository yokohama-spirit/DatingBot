using DatingBotLibrary.Domain.Interfaces;
using DatingBotLibrary.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatingBotLibrary.Infrastructure.Repos
{
    public class LikesRepository : ILikesRepository
    {
        private readonly DatabaseConnect _conn;
        private readonly IProfileRepository _repo;

        public LikesRepository
            (DatabaseConnect conn, 
            IProfileRepository repo)
        {
            _conn = conn;
            _repo = repo;
        }


        public async Task UpdateProfileForLike(long myId, long likeId)
        {
            var profile = await _repo.CheckMyProfile(myId);


            var likesSet = new HashSet<long>(profile.Likes);

            if (likesSet.Add(likeId)) 
            {
                profile.Likes.Add(likeId); 
                await _conn.SaveChangesAsync();
            }
        }

        public async Task DeleteProfileLike(long myId, long likeId)
        {
            var profile = await _repo.CheckMyProfile(likeId);

            profile.Likes.Remove(myId);
            await _conn.SaveChangesAsync();
        }
    }
}
