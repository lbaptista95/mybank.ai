using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AccountService.Application.DTOs;
using AccountService.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AccountService.IntegrationTests.Controllers;

public class AccountsControllerTests : IClassFixture<AccountServiceWebFactory>
{
    private readonly HttpClient _client;

    public AccountsControllerTests(AccountServiceWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email = $"acc_{Guid.NewGuid()}@test.com";
        var resp = await _client.PostAsJsonAsync("/users/register",
            new RegisterUserRequest("Account User", email, "StrongPass123!"));
        var body = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    [Fact]
    public async Task CreateAccount_Authenticated_ShouldReturn201()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/accounts", new CreateAccountRequest("BRL"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AccountResponse>();
        body.Should().NotBeNull();
        body!.Currency.Should().Be("BRL");
        body.Balance.Should().Be(0m);
    }

    [Fact]
    public async Task CreateAccount_Unauthenticated_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/accounts", new CreateAccountRequest("BRL"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_OwnAccount_ShouldReturn200()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResp = await _client.PostAsJsonAsync("/accounts", new CreateAccountRequest("BRL"));
        var account = await createResp.Content.ReadFromJsonAsync<AccountResponse>();

        var response = await _client.GetAsync($"/accounts/{account!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountResponse>();
        body!.Id.Should().Be(account.Id);
    }

    [Fact]
    public async Task GetById_NonexistentAccount_ShouldReturn404()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/accounts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyAccounts_ShouldReturnOnlyOwnAccounts()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/accounts", new CreateAccountRequest("BRL"));
        await _client.PostAsJsonAsync("/accounts", new CreateAccountRequest("USD"));

        var response = await _client.GetAsync("/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountResponse>>();
        accounts.Should().HaveCount(2);
    }
}
