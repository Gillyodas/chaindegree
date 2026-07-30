using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Recruitment.Commands.ApplyForJob;
using ChainDegree.Core.Application.Recruitment.Commands.PostJob;
using ChainDegree.Core.Application.Recruitment.Queries.GetJobs;
using ChainDegree.SharedKernel.DomainErrors.Applications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChainDegree.API.Controllers
{
    [ApiController]
    [Route("api/v1/recruitment")]
    public class RecruitmentController : ApiControllerBase
    {
        private readonly ISender _sender;

        public RecruitmentController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Post a new job opportunity with optional degree filters (US-6)
        /// </summary>
        [HttpPost("jobs")]
        [Authorize(Roles = Roles.Recruiter)]
        [ProducesResponseType(typeof(PostJobResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> PostJob([FromBody] PostJobCommand command, CancellationToken ct)
        {
            var result = await _sender.Send(command, ct);
            if (result.IsSuccess)
            {
                return StatusCode(StatusCodes.Status201Created, result.Value);
            }
            return HandleFailure(result);
        }

        /// <summary>
        /// Apply for a job using an issued degree (US-7)
        /// </summary>
        [HttpPost("applications")]
        [Authorize(Roles = Roles.Student)]
        [ProducesResponseType(typeof(ApplyForJobResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ApplyForJob([FromBody] ApplyForJobCommand command, CancellationToken ct)
        {
            var result = await _sender.Send(command, ct);
            if (result.IsSuccess)
            {
                return StatusCode(StatusCodes.Status201Created, result.Value);
            }

            if (result.Error.Code == ApplicationErrors.FilterCriteriaNotSatisfied.Code)
            {
                return UnprocessableEntity(new
                {
                    errorCode = result.Error.Code,
                    detail = result.Error.Message
                });
            }

            return HandleFailure(result);
        }

        /// <summary>
        /// Get active jobs ranked by reputation-weighted JobScore algorithm (US-7)
        /// </summary>
        [HttpGet("jobs")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IReadOnlyList<JobResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetJobs([FromQuery] string? searchTerm, CancellationToken ct)
        {
            var query = new GetJobsQuery(searchTerm);
            var result = await _sender.Send(query, ct);
            return ProcessResult(result);
        }
    }
}
