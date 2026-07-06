using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.API.Contracts.Degrees;
using ChainDegree.API.Filters;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Degrees.Commands.IssueDegree;
using ChainDegree.Core.Application.Degrees.Commands.RetryDegreeConfirmation;
using ChainDegree.Core.Application.Degrees.Queries.GetBatchStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChainDegree.API.Controllers
{
    [ApiController]
    [Route("api/v1/institutions/degrees")]
    public class DegreesController : ApiControllerBase
    {
        private readonly ISender _sender;

        public DegreesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        [Authorize(Policy = Roles.Registrar)]
        [ServiceFilter(typeof(IdempotencyFilterAttribute))]
        [ProducesResponseType(typeof(IssueDegreeResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> IssueDegrees(
            [FromBody] IssueDegreeRequest request,
            CancellationToken ct)
        {
            var items = request.Degrees.Select(d => new IssueDegreeItemDto(
                d.StudentId,
                d.Major,
                d.Classification,
                d.IssuedAt
            )).ToList();

            var command = new IssueDegreeCommand(items);
            var result = await _sender.Send(command, ct);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Accepted(result.Value);
        }

        [HttpGet("batches/{batchId:guid}")]
        [Authorize(Policy = Roles.Registrar)]
        public async Task<IActionResult> GetBatchStatus(
            Guid batchId,
            CancellationToken ct)
        {
            var query = new GetBatchStatusQuery(batchId);
            var result = await _sender.Send(query, ct);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok(result.Value);
        }

        [HttpPost("{id:guid}/retry")]
        [Authorize(Policy = Roles.Registrar)]
        public async Task<IActionResult> RetryDegreeConfirmation(
            Guid id,
            CancellationToken ct)
        {
            var command = new RetryDegreeConfirmationCommand(id);
            var result = await _sender.Send(command, ct);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Accepted();
        }
    }
}
