using System;
using System.Collections.Generic;
using ChainDegree.Core.Domain.Jobs.Enums;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Recruitment.Commands.PostJob
{
    public record DegreeFilterDto(
        DegreeTypeEnum DegreeType,
        string RequiredMajor,
        string MinimumClassification
    );

    public record PostJobCommand(
        Guid CompanyId,
        Guid? PartnerUniversityId,
        string Title,
        string Description,
        decimal SalaryMin,
        decimal SalaryMax,
        DateTime? ApplicationStartDate,
        DateTime ApplicationEndDate,
        List<DegreeFilterDto>? DegreeFilters
    ) : IRequest<Result<PostJobResponse>>;

    public record PostJobResponse(
        Guid JobId,
        string Status,
        DateTime CreatedAt
    );
}
