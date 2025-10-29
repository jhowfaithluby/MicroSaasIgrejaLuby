using AutoMapper;
using MediatR;
using MicroSaas.Application.Common.Interfaces;
using MicroSaas.Application.Members.Dtos;

namespace MicroSaas.Application.Members.Queries.GetMembers;

public sealed class GetMembersQueryHandler : IRequestHandler<GetMembersQuery, IReadOnlyCollection<MemberDto>>
{
    private readonly IMemberRepository _repository;
    private readonly IMapper _mapper;

    public GetMembersQueryHandler(IMemberRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<MemberDto>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await _repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return members.Select(_mapper.Map<MemberDto>).ToArray();
    }
}
