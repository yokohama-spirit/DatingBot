using DatingBotLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DatingBotLibrary.Infrastructure.Data
{
    public class DatabaseConnect : DbContext
    {
        public DatabaseConnect(DbContextOptions<DatabaseConnect> options) : base(options) { }

        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Video> Videos { get; set; }
    }
}
