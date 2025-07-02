using AutoMapper;
using DatingBotLibrary.Application.Requests;
using DatingBotLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Profile = DatingBotLibrary.Domain.Entities.Profile;

namespace DatingBotLibrary.Infrastructure.Data.Mapper
{
    public class UserProfile : AutoMapper.Profile
    {
        public UserProfile()
        {
            CreateMap<CreateProfileRequest, Profile>()
                .ForMember(dest => dest.Photos, opt => opt.MapFrom(src =>
                    src.Photos.Select(p => new Photo { FileId = p.FileId }).ToList()));
        }
    }
}
