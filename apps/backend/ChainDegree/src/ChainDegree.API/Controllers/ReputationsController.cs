using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Reputation.Queries.GetInstitutionReputation;
using ChainDegree.Core.Application.Reputation.Queries.GetReputationHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChainDegree.API.Controllers;

[ApiController]
[Route("api/v1/reputation")]
public class ReputationsController : ApiControllerBase
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
}
