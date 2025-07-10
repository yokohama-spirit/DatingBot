using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatingBotLibrary.Domain.Interfaces
{
    public interface IStartRepository
    {
        Task<bool> isExistsProfile(long chatId);
    }
}
