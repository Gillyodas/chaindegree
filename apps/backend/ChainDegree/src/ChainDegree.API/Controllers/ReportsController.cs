using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.API.Contracts.Reports;
using ChainDegree.API.Extensions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Reports.Commands.ApproveReport;
using ChainDegree.Core.Application.Reports.Commands.RejectReport;
using ChainDegree.Core.Application.Reports.Commands.SubmitReport;
using ChainDegree.Core.Application.Reports.Queries.GetEvidence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChainDegree.API.Controllers
{
    [ApiController]
    [Route("api/v1/institutions")]
    public class ReportsController : ApiControllerBase
    {
        private readonly ISender _sender;

        public ReportsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("degrees/reports")]
        [Authorize(Roles = $"{Roles.Student},{Roles.Recruiter}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5_242_880)] // 5MB limit
        [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880, ValueCountLimit = 10)]
        [EnableRateLimiting(RateLimitPolicies.Reports.Submit)]
        [ProducesResponseType(typeof(SubmitReportResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> SubmitReport([FromForm] SubmitReportRequest request, CancellationToken ct)
        {
            if (request.EvidenceFile == null || request.EvidenceFile.Length == 0)
            {
                return UnprocessableEntity(new { errorCode = "Report.EvidenceRequired", message = "Evidence file is required." });
            }

            await using var stream = request.EvidenceFile.OpenReadStream();
            var command = new SubmitReportCommand(
                request.DegreeId,
                request.ReportType,
                request.Description,
                stream,
                request.EvidenceFile.ContentType,
                request.EvidenceFile.FileName);

            var result = await _sender.Send(command, ct);
            if (result.IsFailure)
            {
                return UnprocessableEntity(new { errorCode = result.Error.Code, message = result.Error.Message });
            }

            return CreatedAtAction(
                nameof(GetReportEvidence),
                new { id = result.Value.ReportId },
                result.Value);
        }

        [HttpGet("reports/{id:guid}/evidence")]
        [Authorize(Roles = $"{Roles.Student},{Roles.Recruiter},{Roles.Admin}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReportEvidence([FromRoute] Guid id, CancellationToken ct)
        {
            var query = new GetEvidenceQuery(id);
            var result = await _sender.Send(query, ct);
            if (result.IsFailure)
            {
                if (result.Error.Code == "Report.UnauthorizedEvidenceDownload")
                {
                    return Forbid();
                }
                return NotFound(new { errorCode = result.Error.Code, message = result.Error.Message });
            }

            return File(result.Value.Stream, result.Value.ContentType, result.Value.DownloadFileName);
        }

        [HttpPost("reports/{id:guid}/approve")]
        [Authorize(Roles = Roles.Admin)]
        [EnableRateLimiting(RateLimitPolicies.Reports.Review)]
        [ProducesResponseType(typeof(ApproveReportResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ApproveReport([FromRoute] Guid id, CancellationToken ct)
        {
            var command = new ApproveReportCommand(id);
            var result = await _sender.Send(command, ct);
            if (result.IsFailure)
            {
                if (result.Error.Code == "Report.NotFound")
                {
                    return NotFound(new { errorCode = result.Error.Code, message = result.Error.Message });
                }
                return Conflict(new { errorCode = result.Error.Code, message = result.Error.Message });
            }

            return Accepted(result.Value);
        }

        [HttpPost("reports/{id:guid}/reject")]
        [Authorize(Roles = Roles.Admin)]
        [EnableRateLimiting(RateLimitPolicies.Reports.Review)]
        [ProducesResponseType(typeof(RejectReportResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RejectReport([FromRoute] Guid id, [FromBody] RejectReportRequest request, CancellationToken ct)
        {
            var command = new RejectReportCommand(id, request.Reason);
            var result = await _sender.Send(command, ct);
            if (result.IsFailure)
            {
                if (result.Error.Code == "Report.NotFound")
                {
                    return NotFound(new { errorCode = result.Error.Code, message = result.Error.Message });
                }
                return UnprocessableEntity(new { errorCode = result.Error.Code, message = result.Error.Message });
            }

            return Accepted(result.Value);
        }
    }
}
