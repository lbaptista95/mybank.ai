using FraudDetectionService.Application.DTOs;
using FraudDetectionService.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace FraudDetectionService.API.Controllers;

[ApiController]
[Route("analyses")]
public class FraudController : ControllerBase
{
    private readonly AnalyzeTransactionUseCase _analyzeUseCase;

    public FraudController(AnalyzeTransactionUseCase analyzeUseCase)
    {
        _analyzeUseCase = analyzeUseCase;
    }

    /// <summary>Get fraud analysis result for a specific transaction.</summary>
    [HttpGet("{transactionId:guid}")]
    [ProducesResponseType(typeof(FraudAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByTransaction(Guid transactionId, CancellationToken ct)
    {
        var result = await _analyzeUseCase.GetByTransactionAsync(transactionId, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
