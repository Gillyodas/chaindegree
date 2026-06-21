using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Jobs;

namespace ChainDegree.Core.Domain.Recruiters.Entities
{
    public class RecruiterAgent
    {
        public Guid Id { get; private set; }
        public Guid CompanyId { get; private set; }
        public Guid UserId { get; private set; }
        public string AgentName { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
    }
}
