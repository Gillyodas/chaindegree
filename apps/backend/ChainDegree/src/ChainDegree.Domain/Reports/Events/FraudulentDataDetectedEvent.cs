using System;
using System.Collections.Generic;
using System.Text;

namespace ChainDegree.Core.Domain.Reports.Events
{
    public class FraudulentDataDetectedEvent
    {
        public Guid EventId { get; private set; }
        public Guid UniversityId { get; private set; }
        public Guid DegreeId { get; private set; }
        public string ViolationType { get; private set; } = null!;
        public Guid ReportId { get; private set; }
        public string ViolationDetails { get; private set; } = null!;
        public DateTime OccurredOn { get; private set; }
    }
}
