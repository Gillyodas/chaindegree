using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Entities;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.Common.Log;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Application.Degrees.Commands.UpdateDegree
{
    public class UpdateDegreeCommandHandler : IRequestHandler<UpdateDegreeCommand, Result<UpdateDegreeResponse>>
    {
        private readonly IDegreeRepository _degreeRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IDegreeHashService _degreeHashService;
        private readonly IBehaviorLogService _behaviorLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateDegreeCommandHandler> _logger;

        public UpdateDegreeCommandHandler(
            IDegreeRepository degreeRepository,
            ICurrentUserAccessor currentUserAccessor,
            IDegreeHashService degreeHashService,
            IBehaviorLogService behaviorLogService,
            IUnitOfWork unitOfWork,
            ILogger<UpdateDegreeCommandHandler> logger)
        {
            _degreeRepository = degreeRepository;
            _currentUserAccessor = currentUserAccessor;
            _degreeHashService = degreeHashService;
            _behaviorLogService = behaviorLogService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<UpdateDegreeResponse>> Handle(
            UpdateDegreeCommand request,
            CancellationToken ct)
        {
            var institutionId = _currentUserAccessor.InstitutionId;
            if (institutionId == null || institutionId == Guid.Empty)
            {
                return Result<UpdateDegreeResponse>.Failure(DegreeErrors.EmptyIdentifiers);
            }

            var degree = await _degreeRepository.GetByIdAsync(request.DegreeId, ct);
            if (degree == null)
            {
                return Result<UpdateDegreeResponse>.Failure(DegreeErrors.NotFound);
            }

            // Authorization ownership check
            if (degree.InstitutionId != institutionId.Value)
            {
                return Result<UpdateDegreeResponse>.Failure(DegreeErrors.InstitutionMismatch);
            }

            if (degree.Status == StatusEnum.Revoked)
            {
                return Result<UpdateDegreeResponse>.Failure(DegreeErrors.InvalidStateTransition);
            }

            var reason = DegreeActionReason.FromCode(request.ReasonCode);
            var isShortcut = degree.Status == StatusEnum.Pending_Confirmation || degree.Status == StatusEnum.Confirmation_Error;

            _logger.LogInformation("[{LogCode}] {Message}. DegreeId={DegreeId}, IsShortcut={IsShortcut}, Reason={ReasonCode}",
                DegreeLogs.Degree_UpdateInitiated.Code,
                DegreeLogs.Degree_UpdateInitiated.Message,
                degree.Id,
                isShortcut,
                reason.Code);

            // Compute crypto snapshot for update
            var degreeData = new DegreeData(
                degree.DegreeCode,
                degree.StudentId,
                request.Major,
                request.Classification,
                degree.IssuedAt);

            CryptoSnapshot cryptoSnapshot;
            try
            {
                cryptoSnapshot = await _degreeHashService.RecalculateAsync(degreeData, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recalculate cryptographic hash. DegreeId={DegreeId}", degree.Id);
                return Result<UpdateDegreeResponse>.Failure(DegreeErrors.InvalidCryptoSnapshot);
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                Result updateResult;
                if (isShortcut)
                {
                    updateResult = degree.UpdateShortcut(request.Major, request.Classification, cryptoSnapshot, reason);
                }
                else
                {
                    updateResult = degree.InitiateUpdate(cryptoSnapshot.DataHashLocal, reason);

                    if (updateResult.IsSuccess)
                    {
                        // Create and store the staging model for confirmed degrees update flow
                        var existingRequest = await _degreeRepository.GetUpdateRequestByDegreeIdAsync(degree.Id, ct);
                        if (existingRequest != null)
                        {
                            _degreeRepository.RemoveUpdateRequest(existingRequest);
                        }

                        var stagingRequest = DegreeUpdateRequest.Create(
                            degree.Id,
                            request.Major,
                            request.Classification,
                            cryptoSnapshot,
                            reason);

                        await _degreeRepository.AddUpdateRequestAsync(stagingRequest, ct);
                    }
                }

                if (updateResult.IsFailure)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<UpdateDegreeResponse>.Failure(updateResult.Error);
                }

                // Audit log
                var serializedLog = System.Text.Json.JsonSerializer.Serialize(new
                {
                    degree.Id,
                    degree.DegreeCode,
                    degree.Status,
                    Reason = reason.Code,
                    NewHash = cryptoSnapshot.DataHashLocal
                });

                await _behaviorLogService.LogAsync(
                    ActionTypeEnum.ALTER_DEGREE,
                    "DEGREES",
                    degree.Id,
                    oldValuesJson: null,
                    newValuesJson: serializedLog,
                    ct);

                await _unitOfWork.CommitAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                _logger.LogInformation("[{LogCode}] {Message}. DegreeId={DegreeId}, FinalStatus={Status}",
                    DegreeLogs.Degree_UpdateCompleted.Code,
                    DegreeLogs.Degree_UpdateCompleted.Message,
                    degree.Id,
                    degree.Status);

                return Result<UpdateDegreeResponse>.Success(new UpdateDegreeResponse(
                    degree.Id,
                    degree.Status.ToString(),
                    isShortcut,
                    isShortcut ? "Degree details updated directly (Shortcut)." : "Degree update request accepted and queued."
                ));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                _logger.LogError(ex, "Error occurred during degree update handler execution. DegreeId={DegreeId}", degree.Id);
                throw;
            }
        }
    }
}
