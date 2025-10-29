using Microsoft.EntityFrameworkCore;
using MicroSaas.Application.Common.Interfaces;
using MicroSaas.Domain.Entities;
using MicroSaas.Infrastructure.Persistence;

namespace MicroSaas.Infrastructure.Repositories;

public sealed class MemberRepository : IMemberRepository
{
    private readonly AppDbContext _context;

    public MemberRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Member> AddAsync(Member member, CancellationToken cancellationToken)
    {
        _context.Members.Add(member);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return member;
    }

    public async Task<IReadOnlyCollection<Member>> ListAsync(CancellationToken cancellationToken)
    {
        var members = await _context.Members
            .OrderBy(member => member.FullName)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return members;
    }
}
