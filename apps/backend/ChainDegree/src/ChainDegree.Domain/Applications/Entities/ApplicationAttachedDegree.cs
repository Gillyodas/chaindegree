using System;

namespace ChainDegree.Core.Domain.Applications.Entities
{
    public class ApplicationAttachedDegree
    {
        public Guid ApplicationId { get; private set; }
        public Guid DegreeId { get; private set; }

        private ApplicationAttachedDegree() { }

        public ApplicationAttachedDegree(Guid applicationId, Guid degreeId)
        {
            ApplicationId = applicationId;
            DegreeId = degreeId;
        }
    }
}
