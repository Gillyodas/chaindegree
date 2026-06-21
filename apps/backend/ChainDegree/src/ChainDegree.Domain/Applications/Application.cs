using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Applications.Enums;

namespace ChainDegree.Core.Domain.Applications
{
    public class Application
    {
        public Guid Id { get; private set; }
        public Guid JobId { get; private set; }
        public Guid StudentId { get; private set; }
        public Guid AttachedDegreeId { get; private set; }
        public ApplicationRankStatusEnum RankStatus { get; private set; }
        public ApplicationProcessStatusEnum ProcessStatus { get; private set; }
        public bool IsForceSubmitted { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

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
