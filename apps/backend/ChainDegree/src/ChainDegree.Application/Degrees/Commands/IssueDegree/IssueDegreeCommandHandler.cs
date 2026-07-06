using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.Common.Log;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Application.Degrees.Commands.IssueDegree
{
    public class IssueDegreeCommandHandler : IRequestHandler<IssueDegreeCommand, Result<IssueDegreeResponse>>
    {
        private readonly IDegreeIssuanceService _issuanceService;
        private readonly IDegreeRepository _degreeRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IBehaviorLogService _behaviorLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IssueDegreeCommandHandler> _logger;

        public IssueDegreeCommandHandler(
            IDegreeIssuanceService issuanceService,
            IDegreeRepository degreeRepository,
            ICurrentUserAccessor currentUserAccessor,
            IBehaviorLogService behaviorLogService,
            IUnitOfWork unitOfWork,
            ILogger<IssueDegreeCommandHandler> logger)
        {
            _issuanceService = issuanceService;
            _degreeRepository = degreeRepository;
            _currentUserAccessor = currentUserAccessor;
            _behaviorLogService = behaviorLogService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<IssueDegreeResponse>> Handle(
            IssueDegreeCommand request,
            CancellationToken ct)
        {
            var institutionId = _currentUserAccessor.InstitutionId;
            var registrarId = _currentUserAccessor.UserId;

            if (institutionId == null || institutionId == Guid.Empty || registrarId == Guid.Empty)
            {
                return Result<IssueDegreeResponse>.Failure(DegreeErrors.EmptyIdentifiers);
            }

            _logger.LogInformation("[{LogCode}] {Message}. InstitutionId={InstitutionId}, DegreesToProcess={Count}",
                DegreeLogs.Degree_IssuanceInitiated.Code,
                DegreeLogs.Degree_IssuanceInitiated.Message,
                institutionId.Value,
                request.Degrees.Count);

            // Execute within a single Transaction Boundary
            await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var partialResult = await _issuanceService.IssueDegreesAsync(
                    institutionId.Value,
                    registrarId,
                    request.Degrees,
                    ct);

                if (partialResult.IsFullFailure)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    _logger.LogWarning("[{LogCode}] {Message}. All degrees failed verification.",
                        DegreeLogs.Degree_IssuancePartialSuccess.Code,
                        DegreeLogs.Degree_IssuancePartialSuccess.Message);

                    return Result<IssueDegreeResponse>.Success(new IssueDegreeResponse(
                        "All degree issuance requests were rejected.",
                        0,
                        Array.Empty<Guid>(),
                        partialResult.Failures
                    ));
                }

                // Save successfully created degrees
                await _degreeRepository.AddRangeAsync(partialResult.Successes, ct);

                // Write Behavior Log for each successfully issued degree
                foreach (var degree in partialResult.Successes)
                {
                    var serializedDegree = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        degree.Id,
                        degree.DegreeCode,
                        degree.StudentId,
                        degree.Major,
                        degree.Classification,
                        degree.IssuedAt,
                        degree.Status
                    });

                    await _behaviorLogService.LogAsync(
                        ActionTypeEnum.CREATE_DEGREE,
                        "DEGREES",
                        degree.Id,
                        oldValuesJson: null,
                        newValuesJson: serializedDegree,
                        ct);
                }

                // Commit the changes to DB and outbox
                await _unitOfWork.CommitAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                var successIds = partialResult.Successes.Select(d => d.Id).ToList();

                var logCode = partialResult.IsFullSuccess 
                    ? DegreeLogs.Degree_IssuanceCompleted 
                    : DegreeLogs.Degree_IssuancePartialSuccess;

                _logger.LogInformation("[{LogCode}] {Message}. InstitutionId={InstitutionId}, SuccessfullyIssued={SuccessCount}, Failed={FailedCount}",
                    logCode.Code,
                    logCode.Message,
                    institutionId.Value,
                    successIds.Count,
                    partialResult.Failures.Count);

                return Result<IssueDegreeResponse>.Success(new IssueDegreeResponse(
                    "Degree issuance request processed successfully.",
                    successIds.Count,
                    successIds,
                    partialResult.Failures
                ));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                _logger.LogError(ex, "Error occurred during degree issuance handler execution.");
                throw;
            }
        }
    }
}
