using System;
using ChainDegree.Core.Domain.Applications;
using ChainDegree.Core.Domain.Applications.Enums;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.Jobs;
using ChainDegree.Core.Domain.Jobs.Entities;
using ChainDegree.Core.Domain.Jobs.Enums;
using ChainDegree.SharedKernel.DomainErrors.Jobs;
using ChainDegree.SharedKernel.Result;
using Moq;
using Xunit;

namespace ChainDegree.Domain.Tests.Recruitment
{
    public class RecruitmentDomainTests
    {
        [Fact]
        public void Job_Create_WithNegativeOrZeroSalary_ReturnsInvalidSalaryRangeError()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;

            // Act
            var resultZero = Job.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Title", "Desc", 0, 100, null, now.AddDays(10).UtcDateTime, now);
            var resultNegative = Job.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Title", "Desc", -10, 100, null, now.AddDays(10).UtcDateTime, now);

            // Assert
            Assert.True(resultZero.IsFailure);
            Assert.Equal(JobErrors.InvalidSalaryRange.Code, resultZero.Error.Code);
            Assert.True(resultNegative.IsFailure);
            Assert.Equal(JobErrors.InvalidSalaryRange.Code, resultNegative.Error.Code);
        }

        [Fact]
        public void Job_Create_WithDescriptionExceeding4000Chars_ReturnsDescriptionTooLongError()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            string longDescription = new string('A', 4001);

            // Act
            var result = Job.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Title", longDescription, 1000, 2000, null, now.AddDays(10).UtcDateTime, now);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(JobErrors.DescriptionTooLong.Code, result.Error.Code);
        }

        [Fact]
        public void JobDegreeFilter_HierarchicalClassification_MatchesCorrectly()
        {
            // Arrange
            var filter = new JobDegreeFilter(Guid.NewGuid(), DegreeTypeEnum.Cu_Nhan, "Computer Science", "Giỏi");

            // Act & Assert
            // 1. Excellent (Xuất sắc) >= Giỏi -> Should pass
            var excellentDegree = CreateMockDegree("Computer Science", "Xuất sắc");
            Assert.True(filter.IsSatisfiedBy(excellentDegree));

            // 2. Good (Giỏi) == Giỏi -> Should pass
            var goodDegree = CreateMockDegree("Computer Science", "Giỏi");
            Assert.True(filter.IsSatisfiedBy(goodDegree));

            // 3. Fair (Khá) < Giỏi -> Should fail
            var fairDegree = CreateMockDegree("Computer Science", "Khá");
            Assert.False(filter.IsSatisfiedBy(fairDegree));
        }

        [Fact]
        public void Job_EvaluateApplication_ReturnsHighlyQualifiedWhenFiltersSatisfied()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Senior Dev", "Description", 1000, 2000, null, now.AddDays(10).UtcDateTime, now).Value;
            job.AddFilter(DegreeTypeEnum.Cu_Nhan, "Software Engineering", "Giỏi");

            var studentDegree = CreateMockDegree("Software Engineering", "Xuất sắc");

            // Act
            var rankStatus = job.EvaluateApplication(studentDegree);

            // Assert
            Assert.Equal(ApplicationRankStatusEnum.Highly_Qualified, rankStatus);
        }

        [Fact]
        public void Application_Create_InitializesWithSubmittedProcessStatus()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var jobId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var degreeId = Guid.NewGuid();

            // Act
            var app = Application.Create(jobId, studentId, degreeId, ApplicationRankStatusEnum.Highly_Qualified, false, now).Value;

            // Assert
            Assert.Equal(ApplicationProcessStatusEnum.Submitted, app.ProcessStatus);
            Assert.Equal(ApplicationRankStatusEnum.Highly_Qualified, app.RankStatus);
            Assert.False(app.IsForceSubmitted);
        }

        [Fact]
        public void Application_SubmitForcefully_SetsUnderQualifiedAndForceSubmittedFlag()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var app = Application.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ApplicationRankStatusEnum.Highly_Qualified, false, now).Value;

            // Act
            app.SubmitForcefully();

            // Assert
            Assert.True(app.IsForceSubmitted);
            Assert.Equal(ApplicationRankStatusEnum.Under_Qualified, app.RankStatus);
        }

        private static Degree CreateMockDegree(string major, string classification)
        {
            var mockHashService = new Mock<IHashService>();
            mockHashService.Setup(h => h.GenerateSalt()).Returns(Result<string>.Success("a7d83bf92c81e3d0"));
            mockHashService.Setup(h => h.HashData(It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(Result<string>.Success("mocked_data_hash_local"));

            var plainJson = "{\"classification\":\"" + classification + "\",\"degreeCode\":\"DEG-2026-000001\",\"major\":\"" + major + "\"}";
            var cryptoData = CryptoSnapshot.Create(plainJson, mockHashService.Object).Value;

            return Degree.Create(
                1,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                major,
                classification,
                cryptoData
            ).Value;
        }
    }
}
