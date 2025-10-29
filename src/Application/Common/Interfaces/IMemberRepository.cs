using MicroSaas.Domain.Entities;

namespace MicroSaas.Application.Common.Interfaces;

public interface IMemberRepository
{
    Task<Member> AddAsync(Member member, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Member>> ListAsync(CancellationToken cancellationToken);
}
