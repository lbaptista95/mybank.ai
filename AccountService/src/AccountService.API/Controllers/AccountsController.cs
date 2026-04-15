using System.Security.Claims;
using AccountService.Application.DTOs;
using AccountService.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.API.Controllers;

[ApiController]
[Route("accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly CreateAccountUseCase _createAccountUseCase;

    public AccountsController(CreateAccountUseCase createAccountUseCase)
    {
        _createAccountUseCase = createAccountUseCase;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User ID claim missing."));

    /// <summary>Create a new account/wallet for the authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request, CancellationToken ct)
    {
        var result = await _createAccountUseCase.ExecuteAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(GetById), new { accountId = result.Id }, result);
    }

    /// <summary>Get a specific account by ID.</summary>
    [HttpGet("{accountId:guid}")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid accountId, CancellationToken ct)
    {
        try
        {
            var result = await _createAccountUseCase.GetByIdAsync(accountId, CurrentUserId, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>List all accounts for the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAccounts(CancellationToken ct)
    {
        var result = await _createAccountUseCase.GetByUserAsync(CurrentUserId, ct);
        return Ok(result);
    }
}
