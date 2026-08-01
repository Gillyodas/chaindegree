using System;
using System.Collections.Generic;
using ChainDegree.Core.Domain.Applications.Entities;
using ChainDegree.Core.Domain.Applications.Enums;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.SharedKernel.DomainErrors.Applications;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Domain.Applications
{
    public class Application : AggregateRoot
    {
        public Guid JobId { get; private set; }
        public Guid StudentId { get; private set; }
        public ApplicationRankStatusEnum RankStatus { get; private set; }
        public ApplicationProcessStatusEnum ProcessStatus { get; private set; }
        public bool IsForceSubmitted { get; private set; }

        private readonly List<ApplicationAttachedDegree> _attachedDegrees = new();
        public IReadOnlyCollection<ApplicationAttachedDegree> AttachedDegrees => _attachedDegrees.AsReadOnly();

        private Application() { }

        private Application(
            Guid id,
            Guid jobId,
            Guid studentId,
            Guid degreeId,
            ApplicationRankStatusEnum rankStatus,
            bool isForceSubmitted,
            DateTime utcNow)
        {
            Id = id;
            JobId = jobId;
            StudentId = studentId;
            RankStatus = rankStatus;
            ProcessStatus = ApplicationProcessStatusEnum.Submitted;
            IsForceSubmitted = isForceSubmitted;
            CreatedAt = utcNow;
            AttachDegree(degreeId, isPrimary: true);
        }

        public static Result<Application> Create(
            Guid jobId,
            Guid studentId,
            Guid degreeId,
            ApplicationRankStatusEnum rankStatus,
            bool isForceSubmitted,
            DateTimeOffset utcNow)
        {
            if (jobId == Guid.Empty || studentId == Guid.Empty || degreeId == Guid.Empty)
                return Result<Application>.Failure(ApplicationErrors.EmptyIdentifiers);

            var application = new Application(
                Guid.NewGuid(),
                jobId,
                studentId,
                degreeId,
                rankStatus,
                isForceSubmitted,
                utcNow.UtcDateTime
            );

            return Result<Application>.Success(application);
        }

        public Result AttachDegree(Guid degreeId, bool isPrimary = false)
        {
            if (degreeId == Guid.Empty)
                return Result.Failure(ApplicationErrors.EmptyIdentifiers);

            if (!_attachedDegrees.Exists(ad => ad.DegreeId == degreeId))
            {
                if (isPrimary && _attachedDegrees.Exists(ad => ad.IsPrimary))
                {
                    return Result.Failure(ApplicationErrors.MultiplePrimaryDegrees);
                }
                _attachedDegrees.Add(new ApplicationAttachedDegree(Id, degreeId, isPrimary));
                UpdatedAt = DateTime.UtcNow;
            }

            return Result.Success();
        }

        public void SubmitForcefully()
        {
            IsForceSubmitted = true;
            RankStatus = ApplicationRankStatusEnum.Under_Qualified;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateProcessStatus(ApplicationProcessStatusEnum newStatus)
        {
            ProcessStatus = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
