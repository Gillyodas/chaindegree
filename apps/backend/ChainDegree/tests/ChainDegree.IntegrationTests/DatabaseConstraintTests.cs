using System;
using System.Threading.Tasks;
using ChainDegree.Core.Infrastructure.Persistence;
using ChainDegree.Core.Infrastructure.Persistence.Entities;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Entities;
using ChainDegree.Core.Domain.Universities;
using ChainDegree.Core.Domain.Universities.Entities;
using ChainDegree.Core.Domain.Students;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Testcontainers.SqlEdge;
using ChainDegree.Core.Domain.Degrees.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using Moq;

namespace ChainDegree.IntegrationTests
{
    public class DatabaseConstraintTests : IAsyncLifetime
    {
        private readonly SqlEdgeContainer _dbContainer = new SqlEdgeBuilder().Build();
        private ChainDegreeDbContext _dbContext = null!;

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();

            var options = new DbContextOptionsBuilder<ChainDegreeDbContext>()
                .UseSqlServer(_dbContainer.GetConnectionString())
                .Options;

            var mockCurrentUser = new Mock<ICurrentUserAccessor>();
            mockCurrentUser.Setup(x => x.InstitutionId).Returns((Guid?)null);

            _dbContext = new ChainDegreeDbContext(options, mockCurrentUser.Object, new NullLogger<ChainDegreeDbContext>());
            await _dbContext.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await _dbContext.DisposeAsync();
            await _dbContainer.DisposeAsync();
        }

        [Fact]
        public async Task BatchRecord_ShouldThrowException_WhenDuplicateMerkleRootInserted()
        {
            // Arrange
            var institution = EducationInstitution.Create("TEST", "Test Inst", "test@test.com");
            _dbContext.EducationInstitutions.Add(institution);
            await _dbContext.SaveChangesAsync();

            var batch1 = new BatchRecord
            {
                Id = Guid.NewGuid(),
                InstitutionId = institution.Id,
                BatchName = "BATCH_1",
                Status = BatchStatus.Completed,
                DegreeCount = 1,
                MerkleRoot = "duplicate_root",
                CreatedAt = DateTime.UtcNow
            };
            
            var batch2 = new BatchRecord
            {
                Id = Guid.NewGuid(),
                InstitutionId = institution.Id,
                BatchName = "BATCH_2",
                Status = BatchStatus.Completed,
                DegreeCount = 1,
                MerkleRoot = "duplicate_root", // SAME ROOT
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.BatchRecords.Add(batch1);
            await _dbContext.SaveChangesAsync();

            // Act
            _dbContext.BatchRecords.Add(batch2);
            Func<Task> action = async () => await _dbContext.SaveChangesAsync();

            // Assert
            await action.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task BatchDegreeRecord_ShouldThrowException_WhenDuplicateDegreeIdAndVersionInserted()
        {
            // Arrange
            var institution = EducationInstitution.Create("TEST", "Test Inst", "test@test.com");
            _dbContext.EducationInstitutions.Add(institution);
            await _dbContext.SaveChangesAsync();

            var batch1 = new BatchRecord
            {
                Id = Guid.NewGuid(),
                InstitutionId = institution.Id,
                BatchName = "BATCH_DEG_1",
                Status = BatchStatus.Completed,
                DegreeCount = 1,
                MerkleRoot = "root_1",
                CreatedAt = DateTime.UtcNow
            };
            
            var batch2 = new BatchRecord
            {
                Id = Guid.NewGuid(),
                InstitutionId = institution.Id,
                BatchName = "BATCH_DEG_2",
                Status = BatchStatus.Completed,
                DegreeCount = 1,
                MerkleRoot = "root_2",
                CreatedAt = DateTime.UtcNow
            };

            var registrarUserId = Guid.NewGuid();
            var registrarUser = (ChainDegree.Core.Domain.Auth.AuthUser)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ChainDegree.Core.Domain.Auth.AuthUser));
            typeof(ChainDegree.Core.Domain.Auth.AuthUser).GetProperty("Id")!.SetValue(registrarUser, registrarUserId);
            typeof(ChainDegree.Core.Domain.Auth.AuthUser).GetProperty("Email")!.SetValue(registrarUser, "reguser@test.com");
            typeof(ChainDegree.Core.Domain.Auth.AuthUser).GetProperty("PasswordHash")!.SetValue(registrarUser, "hash");

            var studentUserId = Guid.NewGuid();
            var studentUser = (ChainDegree.Core.Domain.Auth.AuthUser)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ChainDegree.Core.Domain.Auth.AuthUser));
            typeof(ChainDegree.Core.Domain.Auth.AuthUser).GetProperty("Id")!.SetValue(studentUser, studentUserId);
            typeof(ChainDegree.Core.Domain.Auth.AuthUser).GetProperty("Email")!.SetValue(studentUser, "stuuser@test.com");
            typeof(ChainDegree.Core.Domain.Auth.AuthUser).GetProperty("PasswordHash")!.SetValue(studentUser, "hash");

            _dbContext.Set<ChainDegree.Core.Domain.Auth.AuthUser>().AddRange(registrarUser, studentUser);

            var registrarId = Guid.NewGuid();
            var registrar = (Registrar)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Registrar));
            typeof(Registrar).GetProperty("Id")!.SetValue(registrar, registrarId);
            typeof(Registrar).GetProperty("UserId")!.SetValue(registrar, registrarUserId);
            typeof(Registrar).GetProperty("InstitutionId")!.SetValue(registrar, institution.Id);
            typeof(Registrar).GetProperty("EmployeeCode")!.SetValue(registrar, "REG123");
            typeof(Registrar).GetProperty("FullName")!.SetValue(registrar, "John Doe");

            var studentId = Guid.NewGuid();
            var student = (Student)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Student));
            typeof(Student).GetProperty("Id")!.SetValue(student, studentId);
            typeof(Student).GetProperty("UserId")!.SetValue(student, studentUserId);
            typeof(Student).GetProperty("IdentityNumber")!.SetValue(student, "ID123");
            typeof(Student).GetProperty("FullName")!.SetValue(student, "Jane Doe");
            typeof(Student).GetProperty("Email")!.SetValue(student, "stu@test.com");

            _dbContext.Set<Registrar>().Add(registrar);
            _dbContext.Set<Student>().Add(student);
            await _dbContext.SaveChangesAsync();

            var crypto = (CryptoSnapshot)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(CryptoSnapshot));
            typeof(CryptoSnapshot).GetProperty("PlainDataJson")!.SetValue(crypto, "plain");
            typeof(CryptoSnapshot).GetProperty("Salt")!.SetValue(crypto, "salt");
            typeof(CryptoSnapshot).GetProperty("DataHashLocal")!.SetValue(crypto, "hash");

            var degreeResult = Degree.Create(0, institution.Id, registrarId, studentId, "Computer Science", "Excellent", crypto);
            var degree = degreeResult.Value;
            
            _dbContext.BatchRecords.AddRange(batch1, batch2);
            _dbContext.Degrees.Add(degree);
            await _dbContext.SaveChangesAsync();

            var record1 = new BatchDegreeRecord
            {
                BatchId = batch1.Id,
                DegreeId = degree.Id,
                Version = 1,
                LeafIndex = 0
            };
            
            var record2 = new BatchDegreeRecord
            {
                BatchId = batch2.Id, // Different batch
                DegreeId = degree.Id,
                Version = 1,         // SAME DegreeId + Version!
                LeafIndex = 0
            };

            _dbContext.BatchDegreeRecords.Add(record1);
            await _dbContext.SaveChangesAsync();

            // Act
            _dbContext.BatchDegreeRecords.Add(record2);
            Func<Task> action = async () => await _dbContext.SaveChangesAsync();

            // Assert
            await action.Should().ThrowAsync<DbUpdateException>();
        }
    }
}
