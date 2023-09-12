using AutoMapper;
using GorgoDresses.Common.Models.Dress;
using GorgoDresses.Entities;

namespace GorgoDresses.Api.Helpers;

public class MapperConfig : Profile
{
    public MapperConfig()
    {
        CreateMap<Dress, DressModel>().ReverseMap();
        CreateMap<Dress, DressAdminModel>().ReverseMap();
        CreateMap<Dress, DressBasicInfoModel>().ReverseMap();
        CreateMap<Dress, DressAdminBasicInfoModel>().ReverseMap();
        CreateMap<Dress, DressTypeModel>().ReverseMap();
        CreateMap<string, DressTypeModel>().ReverseMap();
    }
}
