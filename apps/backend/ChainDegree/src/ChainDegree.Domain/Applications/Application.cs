using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Applications.Enums;
using ChainDegree.Core.Domain.Applications.Entities;

namespace ChainDegree.Core.Domain.Applications
{
    public class Application
    {
        public Guid Id { get; private set; }
        public Guid JobId { get; private set; }
        public Guid StudentId { get; private set; }
        public ApplicationRankStatusEnum RankStatus { get; private set; }
        public ApplicationProcessStatusEnum ProcessStatus { get; private set; }
        public bool IsForceSubmitted { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private readonly List<ApplicationAttachedDegree> _attachedDegrees = new();
        public IReadOnlyCollection<ApplicationAttachedDegree> AttachedDegrees => _attachedDegrees.AsReadOnly();

        public void AttachDegree(Guid degreeId)
        {
            if (degreeId == Guid.Empty)
                throw new ArgumentException("DegreeId cannot be empty.");

            if (!_attachedDegrees.Exists(ad => ad.DegreeId == degreeId))
            {
                _attachedDegrees.Add(new ApplicationAttachedDegree(this.Id, degreeId));
                this.UpdatedAt = DateTime.UtcNow;
            }
        }

        public void SubmitForcefully()
        {
            throw new NotImplementedException();
        }

        public void UpdateProcessStatus(ApplicationProcessStatusEnum newStatus)
        {
            throw new NotImplementedException();
        }
    }
}
