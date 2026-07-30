namespace ChainDegree.Core.Application.Recruitment.Options
{
    public class RankingOptions
    {
        public const string SectionName = "RankingOptions";

        public double WeightSalary { get; set; } = 40.0;
        public double WeightReputation { get; set; } = 60.0;
        public double WeightTime { get; set; } = 100.0;
        public int DefaultReputationScore { get; set; } = 500;
    }
}
