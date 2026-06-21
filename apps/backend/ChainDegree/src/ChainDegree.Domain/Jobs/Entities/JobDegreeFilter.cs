using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Jobs.Enums;

namespace ChainDegree.Core.Domain.Jobs.Entities
{
    public class JobDegreeFilter
    {
        public Guid Id { get; private set; }
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
