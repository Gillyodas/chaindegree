using System;
using System.Collections.Generic;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Recruitment.Queries.GetJobs
{
    public record GetJobsQuery(
        string? SearchTerm = null
    ) : IRequest<Result<IReadOnlyList<JobResponse>>>;

    public record JobResponse(
        Guid Id,
        Guid CompanyId,
        Guid? PartnerUniversityId,
        string Title,
        string Description,
        decimal SalaryMin,
        decimal SalaryMax,
        DateTime ApplicationStartDate,
        DateTime ApplicationEndDate,
        string Status,
        double JobScore,
        DateTime CreatedAt
    );
}
