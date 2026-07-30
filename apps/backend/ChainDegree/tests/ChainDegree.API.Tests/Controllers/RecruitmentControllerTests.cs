using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.API.Controllers;
using ChainDegree.Core.Application.Recruitment.Commands.ApplyForJob;
using ChainDegree.Core.Application.Recruitment.Commands.PostJob;
using ChainDegree.Core.Application.Recruitment.Queries.GetJobs;
using ChainDegree.SharedKernel.DomainErrors.Applications;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ChainDegree.API.Tests.Controllers
{
    public class RecruitmentControllerTests
    {
        private readonly Mock<ISender> _mockSender;
        private readonly RecruitmentController _controller;

        public RecruitmentControllerTests()
        {
            _mockSender = new Mock<ISender>();
            _controller = new RecruitmentController(_mockSender.Object);
        }

        [Fact]
        public async Task PostJob_ValidRequest_Returns201Created()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var command = new PostJobCommand(Guid.NewGuid(), null, "Title", "Desc", 1000, 2000, null, DateTime.UtcNow.AddDays(5), null);
            var response = new PostJobResponse(jobId, "Active", DateTime.UtcNow);

            _mockSender.Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<PostJobResponse>.Success(response));

            // Act
            var result = await _controller.PostJob(command, CancellationToken.None);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
            Assert.Equal(response, objectResult.Value);
        }

        [Fact]
        public async Task ApplyForJob_ValidRequest_Returns201Created()
        {
            // Arrange
            var appId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var command = new ApplyForJobCommand(jobId, Guid.NewGuid(), false);
            var response = new ApplyForJobResponse(appId, jobId, studentId, "Highly_Qualified", "Submitted", false);

            _mockSender.Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<ApplyForJobResponse>.Success(response));

            // Act
            var result = await _controller.ApplyForJob(command, CancellationToken.None);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
            Assert.Equal(response, objectResult.Value);
        }

        [Fact]
        public async Task ApplyForJob_FilterCriteriaNotSatisfied_Returns422UnprocessableEntity()
        {
            // Arrange
            var command = new ApplyForJobCommand(Guid.NewGuid(), Guid.NewGuid(), false);

            _mockSender.Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<ApplyForJobResponse>.Failure(ApplicationErrors.FilterCriteriaNotSatisfied));

            // Act
            var result = await _controller.ApplyForJob(command, CancellationToken.None);

            // Assert
            var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
            Assert.Equal(StatusCodes.Status422UnprocessableEntity, unprocessableResult.StatusCode);
        }

        [Fact]
        public async Task GetJobs_Returns200OKWithJobList()
        {
            // Arrange
            var jobs = new List<JobResponse>
            {
                new JobResponse(Guid.NewGuid(), Guid.NewGuid(), null, "Job 1", "Desc", 1000, 2000, DateTime.UtcNow, DateTime.UtcNow.AddDays(5), "Active", 150.5, DateTime.UtcNow)
            };

            _mockSender.Setup(s => s.Send(It.IsAny<GetJobsQuery>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<IReadOnlyList<JobResponse>>.Success(jobs));

            // Act
            var result = await _controller.GetJobs(null, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        }
    }
}
