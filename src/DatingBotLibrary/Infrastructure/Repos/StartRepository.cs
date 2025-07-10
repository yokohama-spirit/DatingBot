using DatingBotLibrary.Domain.Interfaces;
using DatingBotLibrary.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatingBotLibrary.Infrastructure.Repos
{
    public class StartRepository : IStartRepository
    {
        private readonly DatabaseConnect _conn;

        public StartRepository
            (DatabaseConnect conn)
        {
            _conn = conn;
        }
        public async Task<bool> isExistsProfile(long chatId)
        {
            var profile = await _conn
                .Profiles
                .FirstOrDefaultAsync(p => p.ChatId == chatId);
            return profile != null;
        }
    }
}
