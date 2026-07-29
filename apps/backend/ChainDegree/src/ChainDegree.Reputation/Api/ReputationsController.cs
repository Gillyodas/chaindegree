using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Reputation.Application.Queries.GetInstitutionReputation;
using ChainDegree.Reputation.Application.Queries.GetReputationHistory;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChainDegree.Reputation.Api;

[ApiController]
[Route("api/v1/reputation")]
public class ReputationsController : ControllerBase
{
    private readonly ISender _sender;

    public ReputationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("institutions/{id:guid}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, NoStore = false)]
    [ProducesResponseType(typeof(ReputationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInstitutionReputation(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetInstitutionReputationQuery(id), ct);
        return ProcessResult(result);
    }

    [HttpGet("institutions/{id:guid}/history")]
    [AllowAnonymous]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, NoStore = false)]
    [ProducesResponseType(typeof(ReputationHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReputationHistory(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetReputationHistoryQuery(id, page, pageSize), ct);
        return ProcessResult(result);
    }

    private IActionResult ProcessResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var statusCode = result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(
            statusCode: statusCode,
            detail: result.Error.Message,
            extensions: new Dictionary<string, object?>
            {
                { "errorCode", result.Error.Code }
            });
    }
}
