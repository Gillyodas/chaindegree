using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.DomainErrors.Reports;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Application.Reports.Commands.RejectReport
{
    public class RejectReportCommandHandler : IRequestHandler<RejectReportCommand, Result<RejectReportResponse>>
    {
        private readonly IReportRepository _reportRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IEvidenceStorageService _evidenceStorageService;
        private readonly IBehaviorLogService _behaviorLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RejectReportCommandHandler> _logger;

        public RejectReportCommandHandler(
            IReportRepository reportRepository,
            ICurrentUserAccessor currentUserAccessor,
            IEvidenceStorageService evidenceStorageService,
            IBehaviorLogService behaviorLogService,
            IUnitOfWork unitOfWork,
            ILogger<RejectReportCommandHandler> logger)
        {
            _reportRepository = reportRepository;
            _currentUserAccessor = currentUserAccessor;
            _evidenceStorageService = evidenceStorageService;
            _behaviorLogService = behaviorLogService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<RejectReportResponse>> Handle(RejectReportCommand request, CancellationToken ct)
        {
            if (!_currentUserAccessor.IsAuthenticated || !string.Equals(_currentUserAccessor.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                return Result<RejectReportResponse>.Failure(ReportErrors.UnauthorizedReporter);
            }

            var report = await _reportRepository.GetByIdAsync(request.ReportId, ct);
            if (report == null)
            {
                return Result<RejectReportResponse>.Failure(ReportErrors.NotFound);
            }

            var rejectResult = report.Reject(request.Reason);
            if (rejectResult.IsFailure)
            {
                return Result<RejectReportResponse>.Failure(rejectResult.Error);
            }

            _reportRepository.Update(report);

            var logPayload = System.Text.Json.JsonSerializer.Serialize(new
            {
                ReportId = report.Id,
                report.TargetDegreeId,
                Status = report.Status.ToString(),
                Reason = report.RejectionReason,
                report.ReviewedAt
            });

            await _behaviorLogService.LogAsync(
                ActionTypeEnum.ALTER_DEGREE,
                "REPORTS",
                report.Id,
                oldValuesJson: null,
                newValuesJson: logPayload,
                ct);

            await _unitOfWork.CommitAsync(ct);

            // Lifecycle Policy: Delete evidence file immediately when report is rejected
            if (!string.IsNullOrEmpty(report.EvidenceFileName))
            {
                try
                {
                    await _evidenceStorageService.DeleteEvidenceAsync(report.EvidenceFileName, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete evidence file {FileName} for rejected report {ReportId}", report.EvidenceFileName, report.Id);
                }
            }

            _logger.LogInformation("Report {ReportId} rejected by Admin {AdminId}", report.Id, _currentUserAccessor.UserId);

            return Result<RejectReportResponse>.Success(new RejectReportResponse(
                "Report rejected successfully.",
                report.Id,
                report.RejectionReason!,
                DateTime.UtcNow));
        }
    }
}
