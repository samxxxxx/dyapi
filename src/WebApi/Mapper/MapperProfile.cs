using AutoMapper;
using DYApi.Infrastructure;
using DYApi.Infrastructure.Configuration;
using DYApi.Models;
using DYApi.Models.Dtos;
using Org.BouncyCastle.Asn1.X509.Qualified;

namespace DYApi
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap(typeof(DYService.aweme_detail), typeof(DYService.aweme_detailDto));
            CreateMap(typeof(DYService.DYData), typeof(DYService.DYDataDto));
            CreateMap<DYApiSettings, DYAPISettingsDto>()
                .ForMember((dest) => dest.Domain, opt => opt.MapFrom(map => map.ShortUrlDomain));//成员映射 将ShortUrlDomain映射到Domain

            CreateMap(typeof(UserParseRecord), typeof(UserParseRecordDto));
        }

    }
}
