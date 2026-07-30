using System;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Enums;
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

        private JobDegreeFilter() { }

        public JobDegreeFilter(Guid jobId, DegreeTypeEnum degreeType, string requiredMajor, string minClassification)
        {
            Id = Guid.NewGuid();
            JobId = jobId;
            DegreeType = degreeType;
            RequiredMajor = requiredMajor;
            MinClassification = minClassification;
        }

        public bool IsSatisfiedBy(Degree degree)
        {
            if (degree == null)
                return false;

            // Revoked degrees never satisfy filters
            if (degree.Status == StatusEnum.Revoked || degree.Status == StatusEnum.Pending_Revocation)
                return false;

            // Major check (case-insensitive)
            if (!string.IsNullOrWhiteSpace(RequiredMajor) &&
                !string.Equals(degree.Major, RequiredMajor, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Hierarchical classification check: degree classification rank >= required min classification rank
            int studentRank = GetClassificationRank(degree.Classification);
            int minRequiredRank = GetClassificationRank(MinClassification);

            return studentRank >= minRequiredRank;
        }

        private static int GetClassificationRank(string classification)
        {
            if (string.IsNullOrWhiteSpace(classification))
                return 0;

            var normalized = classification.Trim().ToLowerInvariant();
            return normalized switch
            {
                "xuất sắc" or "excellent" => 4,
                "giỏi" or "good" => 3,
                "khá" or "above average" or "fair" => 2,
                "trung bình" or "average" or "pass" => 1,
                _ => 0
            };
        }
    }
}
