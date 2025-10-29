using MediatR;
using MicroSaas.Application.Members.Dtos;

namespace MicroSaas.Application.Members.Queries.GetMembers;

public sealed record GetMembersQuery() : IRequest<IReadOnlyCollection<MemberDto>>;
