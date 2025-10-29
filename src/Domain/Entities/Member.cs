namespace MicroSaas.Domain.Entities;

public class Member
{
    private Member()
    {
    }

    public Member(Guid id, string fullName, string email, string phone, DateOnly? birthDate)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone.Trim();
        BirthDate = birthDate;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public DateOnly? BirthDate { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public void UpdateContact(string fullName, string email, string phone, DateOnly? birthDate)
    {
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone.Trim();
        BirthDate = birthDate;
    }
}
