using System;
using ChainDegree.Core.Domain.SharedKernel;

namespace ChainDegree.Core.Domain.Recruiters.Entities
{
    public class RecruiterAgent : Entity
    {
        public Guid CompanyId { get; private set; }
        public Guid UserId { get; private set; }
        public string AgentName { get; private set; } = null!;
    }
}
