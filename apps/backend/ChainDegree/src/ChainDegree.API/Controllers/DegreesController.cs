using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.API.Contracts.Degrees;
using ChainDegree.API.Extensions;
using ChainDegree.API.Filters;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Degrees.Commands.IssueDegree;
using ChainDegree.Core.Application.Degrees.Commands.RetryDegreeConfirmation;
using ChainDegree.Core.Application.Degrees.Queries.GetBatchStatus;
using ChainDegree.Core.Application.Degrees.Commands.UpdateDegree;
using ChainDegree.Core.Application.Degrees.Commands.RevokeDegree;
using ChainDegree.Core.Application.Degrees.Queries.VerifyDegree;
using ChainDegree.Core.Application.Degrees.Queries.DTOs;
using ChainDegree.Core.Application.Degrees.Queries.GetDegrees;
using ChainDegree.Core.Application.Degrees.Queries.GetDegreeById;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        [EnableRateLimiting(RateLimitPolicies.Degrees.Issue)]
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

        [HttpGet]
        [Authorize(Policy = Roles.Registrar)]
        [EnableRateLimiting(RateLimitPolicies.Degrees.Read)]
        [ProducesResponseType(typeof(PagedResult<DegreeListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDegrees(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var query = new GetDegreesQuery(pageIndex, pageSize);
            var result = await _sender.Send(query, ct);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok(result.Value);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = Roles.Registrar)]
        [EnableRateLimiting(RateLimitPolicies.Degrees.Read)]
        [ProducesResponseType(typeof(DegreeDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDegreeById(
            Guid id,
            CancellationToken ct = default)
        {
            var query = new GetDegreeByIdQuery(id);
            var result = await _sender.Send(query, ct);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok(result.Value);
        }

        [HttpGet("batches/{batchId:guid}")]
        [Authorize(Policy = Roles.Registrar)]
        [EnableRateLimiting(RateLimitPolicies.Degrees.BatchStatus)]
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
        [EnableRateLimiting(RateLimitPolicies.Degrees.Retry)]
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

        [HttpPost("{id:guid}/revoke")]
        [Authorize(Policy = Roles.Registrar)]
        [EnableRateLimiting(RateLimitPolicies.Degrees.Revoke)]
        [ProducesResponseType(typeof(RevokeDegreeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RevokeDegreeResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeDegree(
            Guid id,
            [FromBody] RevokeDegreeRequest request,
            CancellationToken ct)
        {
            var command = new RevokeDegreeCommand(id, request.ReasonCode);
            var result = await _sender.Send(command, ct);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            if (result.Value.IsShortcut)
            {
                return Ok(result.Value);
            }

            return Accepted(result.Value);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Roles.Registrar)]
        [EnableRateLimiting(RateLimitPolicies.Degrees.Update)]
        [ProducesResponseType(typeof(UpdateDegreeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(UpdateDegreeResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDegree(
            Guid id,
            [FromBody] UpdateDegreeRequest request,
            CancellationToken ct)
        {
            var command = new UpdateDegreeCommand(id, request.Major, request.Classification, request.ReasonCode);
            var result = await _sender.Send(command, ct);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            if (result.Value.IsShortcut)
            {
                return Ok(result.Value);
            }

            return Accepted(result.Value);
        }

        [HttpPost("verify")]
        [AllowAnonymous]
        [RequestSizeLimit(65_536)]
        [EnableRateLimiting(RateLimitPolicies.Degrees.Verify)]
        [ProducesResponseType(typeof(VerifyDegreeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(VerifyDegreeErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(VerifyDegreeErrorResponse), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(VerifyDegreeErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyDegree(
            [FromBody] VerifyDegreeRequest request,
            CancellationToken ct)
        {
            var query = new VerifyDegreeQuery(request.DegreeCode, request.Version, request.IssuedAt, request.PlainDataJson, request.Salt);
            var result = await _sender.Send(query, ct);

            if (result.IsFailure)
            {
                if (result.Error == DegreeErrors.InvalidSaltFormat)
                {
                    return BadRequest(new VerifyDegreeErrorResponse(false, "INVALID_SALT_FORMAT", "Salt must be a 16-character hexadecimal string."));
                }
                if (result.Error == DegreeErrors.CryptoHashMismatch)
                {
                    return UnprocessableEntity(new VerifyDegreeErrorResponse(false, "CRYPTO_HASH_MISMATCH", "Verification failed. The provided data does not match the official records."));
                }
                if (result.Error == DegreeErrors.BlockchainInvalid)
                {
                    return UnprocessableEntity(new VerifyDegreeErrorResponse(false, "BLOCKCHAIN_INVALID", "Verification failed. The degree record could not be validated against the blockchain."));
                }
                if (result.Error == DegreeErrors.NotFound)
                {
                    return NotFound(new VerifyDegreeErrorResponse(false, "DEGREE_NOT_FOUND", "No degree found with the specified code."));
                }
                if (result.Error == DegreeErrors.UnsupportedVersion)
                {
                    return NotFound(new VerifyDegreeErrorResponse(false, "UNSUPPORTED_VERSION", "The specified version does not exist for this degree."));
                }

                return HandleFailure(result);
            }

            return Ok(result.Value);
        }
    }
}
