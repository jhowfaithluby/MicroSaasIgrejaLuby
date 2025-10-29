using MediatR;
using MicroSaas.Application.Members.Dtos;

namespace MicroSaas.Application.Members.Commands.CreateMember;

public sealed record CreateMemberCommand(string FullName, string Email, string Phone, DateOnly? BirthDate) : IRequest<MemberDto>;
