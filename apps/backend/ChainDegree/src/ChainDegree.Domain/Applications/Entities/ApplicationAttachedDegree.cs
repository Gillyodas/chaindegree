using System;

namespace ChainDegree.Core.Domain.Applications.Entities
{
    public class ApplicationAttachedDegree
    {
        public Guid ApplicationId { get; private set; }
        public Guid DegreeId { get; private set; }
        public bool IsPrimary { get; private set; }

        private ApplicationAttachedDegree() { }

        public ApplicationAttachedDegree(Guid applicationId, Guid degreeId, bool isPrimary = false)
        {
            ApplicationId = applicationId;
            DegreeId = degreeId;
            IsPrimary = isPrimary;
        }
    }
}
