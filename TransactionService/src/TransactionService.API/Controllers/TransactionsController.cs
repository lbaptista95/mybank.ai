using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransactionService.Application.DTOs;
using TransactionService.Application.UseCases;
using TransactionService.Domain.Exceptions;

namespace TransactionService.API.Controllers;

[ApiController]
[Route("transactions")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly CreateTransactionUseCase _createTransactionUseCase;

    public TransactionsController(CreateTransactionUseCase createTransactionUseCase)
    {
        _createTransactionUseCase = createTransactionUseCase;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User ID claim missing."));

    /// <summary>Initiate a transfer between accounts.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _createTransactionUseCase.ExecuteAsync(request, ct);
            return AcceptedAtAction(nameof(GetById), new { transactionId = result.Id }, result);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Get a transaction by ID.</summary>
    [HttpGet("{transactionId:guid}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid transactionId, CancellationToken ct)
    {
        var result = await _createTransactionUseCase.GetByIdAsync(transactionId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Get paginated account statement using cursor-based pagination.
    /// Pass the returned <c>nextCursor</c> as the <c>cursor</c> query parameter for the next page.
    /// </summary>
    [HttpGet("{accountId:guid}/statement")]
    [ProducesResponseType(typeof(StatementResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatement(
        Guid accountId,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _createTransactionUseCase.GetStatementAsync(accountId, cursor, pageSize, ct);
        return Ok(result);
    }
}
