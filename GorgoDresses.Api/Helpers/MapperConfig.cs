using AutoMapper;
using GorgoDresses.Common.Models.Dress;
using GorgoDresses.Entities;

namespace GorgoDresses.Api.Helpers;

public class MapperConfig : Profile
{
    public MapperConfig()
    {
        CreateMap<Dress, DressModel>().ReverseMap();
    }
}
