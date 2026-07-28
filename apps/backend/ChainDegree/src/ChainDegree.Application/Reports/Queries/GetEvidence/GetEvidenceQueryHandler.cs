using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.SharedKernel.DomainErrors.Reports;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Reports.Queries.GetEvidence
{
    public class GetEvidenceQueryHandler : IRequestHandler<GetEvidenceQuery, Result<GetEvidenceResponse>>
    {
        private readonly IReportRepository _reportRepository;
        private readonly IEvidenceStorageService _evidenceStorageService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetEvidenceQueryHandler(
            IReportRepository reportRepository,
            IEvidenceStorageService evidenceStorageService,
            ICurrentUserAccessor currentUserAccessor)
        {
            _reportRepository = reportRepository;
            _evidenceStorageService = evidenceStorageService;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<Result<GetEvidenceResponse>> Handle(GetEvidenceQuery request, CancellationToken ct)
        {
            if (!_currentUserAccessor.IsAuthenticated)
            {
                return Result<GetEvidenceResponse>.Failure(ReportErrors.UnauthorizedEvidenceDownload);
            }

            var report = await _reportRepository.GetByIdAsync(request.ReportId, ct);
            if (report == null || string.IsNullOrEmpty(report.EvidenceFileName))
            {
                return Result<GetEvidenceResponse>.Failure(ReportErrors.NotFound);
            }

            var currentUserId = _currentUserAccessor.UserId;
            var currentRole = _currentUserAccessor.Role;

            // Authorization Ownership Check:
            // Admin can download any evidence.
            // Student/Recruiter can download only evidence from reports submitted by themselves.
            var isAdmin = string.Equals(currentRole, Roles.Admin, StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && report.ReporterId != currentUserId)
            {
                return Result<GetEvidenceResponse>.Failure(ReportErrors.UnauthorizedEvidenceDownload);
            }

            var fileResult = await _evidenceStorageService.GetEvidenceAsync(report.EvidenceFileName, ct);
            if (fileResult == null)
            {
                return Result<GetEvidenceResponse>.Failure(ReportErrors.NotFound);
            }

            var (stream, contentType, downloadFileName) = fileResult.Value;

            return Result<GetEvidenceResponse>.Success(new GetEvidenceResponse(stream, contentType, downloadFileName));
        }
    }
}
