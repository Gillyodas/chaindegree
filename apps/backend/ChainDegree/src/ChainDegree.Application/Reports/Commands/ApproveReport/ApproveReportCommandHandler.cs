using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.Reports.Enums;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.DomainErrors.Reports;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Application.Reports.Commands.ApproveReport
{
    public class ApproveReportCommandHandler : IRequestHandler<ApproveReportCommand, Result<ApproveReportResponse>>
    {
        private readonly IReportRepository _reportRepository;
        private readonly IDegreeRepository _degreeRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IBehaviorLogService _behaviorLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ApproveReportCommandHandler> _logger;

        public ApproveReportCommandHandler(
            IReportRepository reportRepository,
            IDegreeRepository degreeRepository,
            ICurrentUserAccessor currentUserAccessor,
            IBehaviorLogService behaviorLogService,
            IUnitOfWork unitOfWork,
            ILogger<ApproveReportCommandHandler> logger)
        {
            _reportRepository = reportRepository;
            _degreeRepository = degreeRepository;
            _currentUserAccessor = currentUserAccessor;
            _behaviorLogService = behaviorLogService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<ApproveReportResponse>> Handle(ApproveReportCommand request, CancellationToken ct)
        {
            if (!_currentUserAccessor.IsAuthenticated || !string.Equals(_currentUserAccessor.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                return Result<ApproveReportResponse>.Failure(ReportErrors.UnauthorizedReporter);
            }

            var report = await _reportRepository.GetByIdAsync(request.ReportId, ct);
            if (report == null)
            {
                return Result<ApproveReportResponse>.Failure(ReportErrors.NotFound);
            }

            var degree = await _degreeRepository.GetByIdAsync(report.TargetDegreeId, ct);
            var universityId = degree?.InstitutionId;

            var approveResult = report.Approve(universityId);
            if (approveResult.IsFailure)
            {
                return Result<ApproveReportResponse>.Failure(approveResult.Error);
            }

            _reportRepository.Update(report);

            var logPayload = System.Text.Json.JsonSerializer.Serialize(new
            {
                ReportId = report.Id,
                report.TargetDegreeId,
                report.ReportType,
                Status = report.Status.ToString(),
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

            _logger.LogInformation("Report {ReportId} approved successfully by Admin {AdminId}", report.Id, _currentUserAccessor.UserId);

            var initiatedProcesses = new List<string> { "ReportApprovedNotification" };
            if (report.ReportType == ReportTypeEnum.Fraudulent_Data)
            {
                initiatedProcesses.Add("ReputationScoreRecalculationEvent");
            }

            return Result<ApproveReportResponse>.Success(new ApproveReportResponse(
                "Report approved successfully. Asynchronous revocation and reputation penalty processes have been initiated.",
                report.Id,
                initiatedProcesses,
                DateTime.UtcNow));
        }
    }
}
