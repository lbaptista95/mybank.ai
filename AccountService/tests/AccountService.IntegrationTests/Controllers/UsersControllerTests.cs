using System.Net;
using System.Net.Http.Json;
using AccountService.Application.DTOs;
using AccountService.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AccountService.IntegrationTests.Controllers;

public class UsersControllerTests : IClassFixture<AccountServiceWebFactory>
{
    private readonly HttpClient _client;

    public UsersControllerTests(AccountServiceWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidRequest_ShouldReturn201WithToken()
    {
        var request = new RegisterUserRequest("Integration User", $"int_{Guid.NewGuid()}@test.com", "StrongPass123!");

        var response = await _client.PostAsJsonAsync("/users/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrEmpty();
        body.User.Email.Should().Be(request.Email.ToLowerInvariant());
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn409()
    {
        var email = $"dup_{Guid.NewGuid()}@test.com";
        var request = new RegisterUserRequest("User A", email, "StrongPass123!");

        await _client.PostAsJsonAsync("/users/register", request);
        var response = await _client.PostAsJsonAsync("/users/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200WithToken()
    {
        var email = $"login_{Guid.NewGuid()}@test.com";
        var password = "StrongPass123!";
        await _client.PostAsJsonAsync("/users/register", new RegisterUserRequest("Login User", email, password));

        var response = await _client.PostAsJsonAsync("/users/login", new LoginRequest(email, password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401()
    {
        var email = $"wrong_{Guid.NewGuid()}@test.com";
        await _client.PostAsJsonAsync("/users/register", new RegisterUserRequest("User", email, "CorrectPass123!"));

        var response = await _client.PostAsJsonAsync("/users/login", new LoginRequest(email, "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/users/login",
            new LoginRequest("nonexistent@test.com", "AnyPass123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
