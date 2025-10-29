using AutoMapper;
using MicroSaas.Application.Members.Dtos;
using MicroSaas.Domain.Entities;

namespace MicroSaas.Application.Common.Mappings;

public sealed class MemberProfile : Profile
{
    public MemberProfile()
    {
        CreateMap<Member, MemberDto>();
    }
}
