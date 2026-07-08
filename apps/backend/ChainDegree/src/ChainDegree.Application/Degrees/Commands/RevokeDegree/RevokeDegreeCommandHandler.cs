using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.Common.Log;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Application.Degrees.Commands.RevokeDegree
{
    public class RevokeDegreeCommandHandler : IRequestHandler<RevokeDegreeCommand, Result<RevokeDegreeResponse>>
    {
        private readonly IDegreeRepository _degreeRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IBehaviorLogService _behaviorLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RevokeDegreeCommandHandler> _logger;

        public RevokeDegreeCommandHandler(
            IDegreeRepository degreeRepository,
            ICurrentUserAccessor currentUserAccessor,
            IBehaviorLogService behaviorLogService,
            IUnitOfWork unitOfWork,
            ILogger<RevokeDegreeCommandHandler> logger)
        {
            _degreeRepository = degreeRepository;
            _currentUserAccessor = currentUserAccessor;
            _behaviorLogService = behaviorLogService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<RevokeDegreeResponse>> Handle(
            RevokeDegreeCommand request,
            CancellationToken ct)
        {
            var institutionId = _currentUserAccessor.InstitutionId;
            if (institutionId == null || institutionId == Guid.Empty)
            {
                return Result<RevokeDegreeResponse>.Failure(DegreeErrors.EmptyIdentifiers);
            }

            var degree = await _degreeRepository.GetByIdAsync(request.DegreeId, ct);
            if (degree == null)
            {
                return Result<RevokeDegreeResponse>.Failure(DegreeErrors.NotFound);
            }

            // Authorization ownership check
            if (degree.InstitutionId != institutionId.Value)
            {
                return Result<RevokeDegreeResponse>.Failure(DegreeErrors.InstitutionMismatch);
            }

            var reason = DegreeActionReason.FromCode(request.ReasonCode);
            var isShortcut = degree.Status == StatusEnum.Pending_Confirmation || degree.Status == StatusEnum.Confirmation_Error;

            _logger.LogInformation("[{LogCode}] {Message}. DegreeId={DegreeId}, IsShortcut={IsShortcut}, Reason={ReasonCode}",
                DegreeLogs.Degree_RevocationInitiated.Code,
                DegreeLogs.Degree_RevocationInitiated.Message,
                degree.Id,
                isShortcut,
                reason.Code);

            await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                Result revokeResult;
                if (isShortcut)
                {
                    revokeResult = degree.RevokeShortcut(reason);
                }
                else
                {
                    revokeResult = degree.InitiateRevocation(reason);
                }

                if (revokeResult.IsFailure)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<RevokeDegreeResponse>.Failure(revokeResult.Error);
                }

                // Audit log
                var serializedLog = System.Text.Json.JsonSerializer.Serialize(new
                {
                    degree.Id,
                    degree.DegreeCode,
                    degree.Status,
                    Reason = reason.Code
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
                    DegreeLogs.Degree_RevocationCompleted.Code,
                    DegreeLogs.Degree_RevocationCompleted.Message,
                    degree.Id,
                    degree.Status);

                return Result<RevokeDegreeResponse>.Success(new RevokeDegreeResponse(
                    degree.Id,
                    degree.Status.ToString(),
                    isShortcut,
                    isShortcut ? "Degree revoked successfully (Shortcut)." : "Degree revocation request accepted and queued."
                ));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                _logger.LogError(ex, "Error occurred during degree revocation handler execution. DegreeId={DegreeId}", degree.Id);
                throw;
            }
        }
    }
}
