using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using TransactionService.Application.DTOs;
using TransactionService.Application.Interfaces;
using TransactionService.IntegrationTests.Fixtures;
using Xunit;

namespace TransactionService.IntegrationTests.Controllers;

public class TransactionsControllerTests : IClassFixture<TransactionServiceWebFactory>
{
    private readonly TransactionServiceWebFactory _factory;
    private readonly HttpClient _client;

    private const string JwtSecret = "test-secret-key-for-integration-tests-minimum-32-chars";
    private const string JwtIssuer = "MyBankAI";
    private const string JwtAudience = "MyBankAI.Clients";

    public TransactionsControllerTests(TransactionServiceWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task CreateTransaction_Authenticated_ShouldReturn202AndPublishKafkaEvent()
    {
        var userId = Guid.NewGuid();
        var fromAccount = Guid.NewGuid();
        var toAccount = Guid.NewGuid();

        _factory.KafkaProducerMock
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(userId));

        var request = new CreateTransactionRequest(fromAccount, toAccount, 250m, "BRL", "Integration test transfer");
        var response = await _client.PostAsJsonAsync("/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        body.Should().NotBeNull();
        body!.FromAccountId.Should().Be(fromAccount);
        body.Amount.Should().Be(250m);
        body.Status.Should().Be("Pending");

        _factory.KafkaProducerMock.Verify(k => k.PublishAsync(
            "transaction.created",
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransaction_Unauthenticated_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/transactions",
            new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 100m, "BRL", null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_ExistingTransaction_ShouldReturn200()
    {
        var userId = Guid.NewGuid();
        _factory.KafkaProducerMock
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(userId));

        var createResp = await _client.PostAsJsonAsync("/transactions",
            new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 50m, "BRL", "Get by ID test"));
        var created = await createResp.Content.ReadFromJsonAsync<TransactionResponse>();

        var response = await _client.GetAsync($"/transactions/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        body!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetById_NonexistentTransaction_ShouldReturn404()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(Guid.NewGuid()));

        var response = await _client.GetAsync($"/transactions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStatement_ShouldReturn200WithPaginatedResults()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        _factory.KafkaProducerMock
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(userId));

        // Create 3 transactions from accountId
        for (var i = 0; i < 3; i++)
        {
            await _client.PostAsJsonAsync("/transactions",
                new CreateTransactionRequest(accountId, Guid.NewGuid(), 10m * (i + 1), "BRL", $"tx {i}"));
        }

        var response = await _client.GetAsync($"/transactions/{accountId}/statement?pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StatementResponse>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(2);
        body.HasMore.Should().BeTrue();
    }
}
