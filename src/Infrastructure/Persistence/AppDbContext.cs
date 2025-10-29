using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MicroSaas.Domain.Entities;
using MicroSaas.Infrastructure.Identity;

namespace MicroSaas.Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Member> Members => Set<Member>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("Members");
            entity.HasKey(member => member.Id);
            entity.Property(member => member.FullName)
                .HasMaxLength(150)
                .IsRequired();
            entity.Property(member => member.Email)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(member => member.Phone)
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(member => member.CreatedAtUtc)
                .IsRequired();
            entity.HasIndex(member => member.Email)
                .IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash)
                .HasMaxLength(256)
                .IsRequired();
            entity.Property(token => token.UserId)
                .IsRequired();
            entity.Property(token => token.ExpiresAtUtc)
                .IsRequired();
            entity.Property(token => token.CreatedAtUtc)
                .IsRequired();
            entity.HasIndex(token => token.TokenHash)
                .IsUnique();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
