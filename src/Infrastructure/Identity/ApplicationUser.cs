using Microsoft.AspNetCore.Identity;

namespace MicroSaas.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
