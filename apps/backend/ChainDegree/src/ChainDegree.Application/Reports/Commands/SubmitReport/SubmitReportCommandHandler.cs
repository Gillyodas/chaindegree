using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Domain.Reports;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.DomainErrors.Reports;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Application.Reports.Commands.SubmitReport
{
    public class SubmitReportCommandHandler : IRequestHandler<SubmitReportCommand, Result<SubmitReportResponse>>
    {
        private readonly IReportRepository _reportRepository;
        private readonly IDegreeRepository _degreeRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IEvidenceStorageService _evidenceStorageService;
        private readonly IBehaviorLogService _behaviorLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SubmitReportCommandHandler> _logger;

        public SubmitReportCommandHandler(
            IReportRepository reportRepository,
            IDegreeRepository degreeRepository,
            ICurrentUserAccessor currentUserAccessor,
            IEvidenceStorageService evidenceStorageService,
            IBehaviorLogService behaviorLogService,
            IUnitOfWork unitOfWork,
            ILogger<SubmitReportCommandHandler> logger)
        {
            _reportRepository = reportRepository;
            _degreeRepository = degreeRepository;
            _currentUserAccessor = currentUserAccessor;
            _evidenceStorageService = evidenceStorageService;
            _behaviorLogService = behaviorLogService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<SubmitReportResponse>> Handle(SubmitReportCommand request, CancellationToken ct)
        {
            if (!_currentUserAccessor.IsAuthenticated)
            {
                return Result<SubmitReportResponse>.Failure(ReportErrors.UnauthorizedReporter);
            }

            var currentUserId = _currentUserAccessor.UserId;
            var currentRole = _currentUserAccessor.Role;

            UserRoleEnum reporterRole;
            if (string.Equals(currentRole, Roles.Student, StringComparison.OrdinalIgnoreCase))
            {
                reporterRole = UserRoleEnum.Student;
            }
            else if (string.Equals(currentRole, Roles.Recruiter, StringComparison.OrdinalIgnoreCase))
            {
                reporterRole = UserRoleEnum.Recruiter;
            }
            else
            {
                return Result<SubmitReportResponse>.Failure(ReportErrors.UnauthorizedReporter);
            }

            var degree = await _degreeRepository.GetByIdAsync(request.TargetDegreeId, ct);
            if (degree == null)
            {
                return Result<SubmitReportResponse>.Failure(ReportErrors.NotFound);
            }

            // Student ownership enforcement: Student can only report their own degree
            if (reporterRole == UserRoleEnum.Student && degree.StudentId != currentUserId)
            {
                return Result<SubmitReportResponse>.Failure(ReportErrors.StudentCannotReportOthersDegree);
            }

            // Anti-spam check: prevent duplicate pending review report by same reporter on same degree
            var hasPendingReport = await _reportRepository.ExistsPendingReportAsync(currentUserId, request.TargetDegreeId, ct);
            if (hasPendingReport)
            {
                return Result<SubmitReportResponse>.Failure(ReportErrors.ReportAlreadyExistsUnderReview);
            }

            // Save evidence file
            var savedFileName = await _evidenceStorageService.SaveEvidenceAsync(
                request.EvidenceStream,
                request.ContentType,
                request.FileName,
                ct);

            try
            {
                var reportResult = Report.Create(
                    request.TargetDegreeId,
                    currentUserId,
                    reporterRole,
                    request.ReportType,
                    request.Description,
                    savedFileName);

                if (reportResult.IsFailure)
                {
                    return Result<SubmitReportResponse>.Failure(reportResult.Error);
                }

                var report = reportResult.Value;

                await _reportRepository.AddAsync(report, ct);

                var logPayload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    ReportId = report.Id,
                    report.TargetDegreeId,
                    report.ReportType,
                    report.ReporterId,
                    ReporterRole = report.ReporterRole.ToString(),
                    report.Status
                });

                await _behaviorLogService.LogAsync(
                    ActionTypeEnum.ALTER_DEGREE,
                    "REPORTS",
                    report.Id,
                    oldValuesJson: null,
                    newValuesJson: logPayload,
                    ct);

                await _unitOfWork.CommitAsync(ct);

                _logger.LogInformation("Report {ReportId} submitted successfully for Degree {DegreeId} by User {UserId}",
                    report.Id, report.TargetDegreeId, currentUserId);

                return Result<SubmitReportResponse>.Success(new SubmitReportResponse(
                    report.Id,
                    report.TargetDegreeId,
                    report.Status.ToString(),
                    report.EvidenceFileName,
                    report.CreatedAt));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process SubmitReportCommand for Degree {DegreeId}. Rolling back saved evidence file.", request.TargetDegreeId);
                await _evidenceStorageService.DeleteEvidenceAsync(savedFileName, ct);
                throw;
            }
        }
    }
}
