using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.Common.Log;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Application.Degrees.Commands.RetryDegreeConfirmation
{
    public class RetryDegreeConfirmationCommandHandler : IRequestHandler<RetryDegreeConfirmationCommand, Result>
    {
        private readonly IDegreeRepository _degreeRepository;
        private readonly IBehaviorLogService _behaviorLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RetryDegreeConfirmationCommandHandler> _logger;

        public RetryDegreeConfirmationCommandHandler(
            IDegreeRepository degreeRepository,
            IBehaviorLogService behaviorLogService,
            IUnitOfWork unitOfWork,
            ILogger<RetryDegreeConfirmationCommandHandler> logger)
        {
            _degreeRepository = degreeRepository;
            _behaviorLogService = behaviorLogService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(RetryDegreeConfirmationCommand request, CancellationToken ct)
        {
            _logger.LogInformation("[{LogCode}] {Message}. DegreeId={DegreeId}",
                DegreeLogs.Degree_RetryInitiated.Code,
                DegreeLogs.Degree_RetryInitiated.Message,
                request.DegreeId);

            var degree = await _degreeRepository.GetByIdAsync(request.DegreeId, ct);
            if (degree == null)
            {
                return Result.Failure(DegreeErrors.StudentNotFound); // Or specific DegreeNotFound if defined
            }

            var retryResult = degree.MarkReadyForRetry();
            if (retryResult.IsFailure)
            {
                return Result.Failure(retryResult.Error);
            }

            var serializedDegree = System.Text.Json.JsonSerializer.Serialize(new
            {
                degree.Id,
                degree.DegreeCode,
                degree.Status,
                degree.UpdatedAt
            });

            await _behaviorLogService.LogAsync(
                ActionTypeEnum.ALTER_DEGREE, // Or a custom RETRY_DEGREE action if defined
                "DEGREES",
                degree.Id,
                oldValuesJson: null,
                newValuesJson: serializedDegree,
                ct);

            await _unitOfWork.CommitAsync(ct);

            return Result.Success();
        }
    }
}
