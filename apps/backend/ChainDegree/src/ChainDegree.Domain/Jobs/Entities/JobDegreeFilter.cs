using System;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Jobs.Enums;
using ChainDegree.Core.Domain.SharedKernel;

namespace ChainDegree.Core.Domain.Jobs.Entities
{
    public class JobDegreeFilter : Entity
    {
        public Guid JobId { get; private set; }
        public DegreeTypeEnum DegreeType { get; private set; }
        public string RequiredMajor { get; private set; } = null!;
        public string MinClassification { get; private set; } = null!;

        public bool isSatisfiedBy(Degree degree)
        {
            throw new NotImplementedException();
        }
    }
}
