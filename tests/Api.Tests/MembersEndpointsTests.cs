using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MicroSaas.Api.Tests.Infrastructure;
using MicroSaas.Application.Members.Commands.CreateMember;
using MicroSaas.Application.Members.Dtos;
using Xunit;

namespace MicroSaas.Api.Tests;

public sealed class MembersEndpointsTests : IClassFixture<TestDatabaseFactory>
{
    private readonly HttpClient _client;

    public MembersEndpointsTests(TestDatabaseFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Should_Create_And_List_Members()
    {
        var token = await RegisterAndAuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var command = new CreateMemberCommand("Maria Silva", "maria@example.com", "+5511999999999", new DateOnly(1990, 5, 10));

        var createResponse = await _client.PostAsJsonAsync("/api/v1/members", command);
        createResponse.EnsureSuccessStatusCode();
        var createdMember = await createResponse.Content.ReadFromJsonAsync<MemberDto>();
        createdMember.Should().NotBeNull();
        createdMember!.Id.Should().NotBe(Guid.Empty);
        createdMember.FullName.Should().Be("Maria Silva");

        var listResponse = await _client.GetAsync("/api/v1/members");
        listResponse.EnsureSuccessStatusCode();
        var members = await listResponse.Content.ReadFromJsonAsync<MemberDto[]>();
        members.Should().NotBeNull();
        members!.Select(m => m.Email).Should().Contain("maria@example.com");
    }

    private async Task<string> RegisterAndAuthenticateAsync()
    {
        var email = $"integration_{Guid.NewGuid():N}@example.com";
        var registerRequest = new RegisterRequest("Test User", email, "Pass@12345");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        tokenResponse.Should().NotBeNull();

        return tokenResponse!.AccessToken;
    }

    private sealed record RegisterRequest(string FullName, string Email, string Password);

    private sealed record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
}
