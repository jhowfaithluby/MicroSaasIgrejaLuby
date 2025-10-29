namespace MicroSaas.Application.Members.Dtos;

public sealed record MemberDto(Guid Id, string FullName, string Email, string Phone, DateOnly? BirthDate, DateTime CreatedAtUtc);
