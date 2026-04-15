using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FraudDetectionService.Application.DTOs;
using FraudDetectionService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FraudDetectionService.Infrastructure.Groq;

/// <summary>
/// Calls the Groq API (OpenAI-compatible) with llama-3.3-70b-versatile
/// to analyze transactions for fraud risk.
/// </summary>
public class GroqService : IGroqService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<GroqService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public GroqService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqService> logger)
    {
        _httpClient = httpClient;
        _model = configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";
        _logger = logger;

        var apiKey = configuration["Groq:ApiKey"]
            ?? throw new InvalidOperationException("Groq:ApiKey is not configured.");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<GroqAnalysisResponse> AnalyzeTransactionAsync(
        TransactionCreatedEvent transaction,
        CancellationToken ct = default)
    {
        var prompt = BuildPrompt(transaction);

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = """
                        You are a financial fraud detection AI. Analyze bank transactions and return a JSON object.
                        Always respond with ONLY valid JSON in this exact format (no markdown, no explanation outside JSON):
                        {
                          "riskScore": <float 0.0-1.0>,
                          "decision": "<Approved|Rejected|ManualReview>",
                          "reasoning": "<brief explanation, max 300 chars>"
                        }
                        Rules:
                        - riskScore >= 0.7 → decision must be Rejected
                        - riskScore 0.4-0.69 → decision should be ManualReview
                        - riskScore < 0.4 → decision should be Approved
                        """
                },
                new { role = "user", content = prompt }
            },
            temperature = 0.1,
            max_tokens = 256,
            response_format = new { type = "json_object" }
        };

        var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Calling Groq API for transaction {TransactionId}", transaction.TransactionId);

        var response = await _httpClient.PostAsync("/chat/completions", content, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        var completion = JsonSerializer.Deserialize<GroqChatCompletion>(responseBody, _jsonOptions)
            ?? throw new InvalidOperationException("Null response from Groq API.");

        var messageContent = completion.Choices[0].Message.Content;
        _logger.LogDebug("Groq response: {Response}", messageContent);

        return ParseGroqResponse(messageContent);
    }

    private static string BuildPrompt(TransactionCreatedEvent tx) =>
        $"""
        Analyze this bank transaction for fraud risk:
        - Transaction ID: {tx.TransactionId}
        - From Account: {tx.FromAccountId}
        - To Account: {tx.ToAccountId}
        - Amount: {tx.Amount} {tx.Currency}
        - Description: {tx.Description ?? "N/A"}
        - Timestamp: {tx.CreatedAt:O}

        Consider: large amounts, unusual patterns, round numbers, suspicious descriptions, timing.
        """;

    private static GroqAnalysisResponse ParseGroqResponse(string content)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<GroqRawResponse>(content, _jsonOptions)
                ?? throw new InvalidOperationException("Could not parse Groq response.");

            return new GroqAnalysisResponse(
                Math.Clamp(parsed.RiskScore, 0m, 1m),
                parsed.Decision ?? "Approved",
                parsed.Reasoning ?? "No reasoning provided."
            );
        }
        catch (JsonException)
        {
            // Fallback if LLM returns non-JSON
            return new GroqAnalysisResponse(0.1m, "Approved", "Could not parse AI response; approved by default.");
        }
    }

    private record GroqRawResponse(decimal RiskScore, string? Decision, string? Reasoning);

    private record GroqChatCompletion(List<GroqChoice> Choices);
    private record GroqChoice(GroqMessage Message);
    private record GroqMessage(string Role, string Content);
}
