using AutoMapper;
using MediatR;
using MicroSaas.Application.Common.Interfaces;
using MicroSaas.Application.Members.Dtos;
using MicroSaas.Domain.Entities;

namespace MicroSaas.Application.Members.Commands.CreateMember;

public sealed class CreateMemberCommandHandler : IRequestHandler<CreateMemberCommand, MemberDto>
{
    private readonly IMemberRepository _repository;
    private readonly IMapper _mapper;

    public CreateMemberCommandHandler(IMemberRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<MemberDto> Handle(CreateMemberCommand request, CancellationToken cancellationToken)
    {
        var member = new Member(Guid.Empty, request.FullName, request.Email, request.Phone, request.BirthDate);
        var created = await _repository.AddAsync(member, cancellationToken).ConfigureAwait(false);
        return _mapper.Map<MemberDto>(created);
    }
}
