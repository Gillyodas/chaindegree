using System;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;
using ChainDegree.SharedKernel.Result;
using ChainDegree.SharedKernel.DomainErrors.Common;

namespace ChainDegree.Core.Domain.Universities.Entities
{
    public class InstitutionStudent : Entity, IInstitutionScoped
    {
        public Guid InstitutionId { get; private set; }
        public Guid StudentId { get; private set; }
        public string StudentCode { get; private set; } = null!;
        public DateTime EnrolledAt { get; private set; }

        private InstitutionStudent() { }

        public static Result<InstitutionStudent> Create(
            Guid institutionId,
            Guid studentId,
            string studentCode)
        {
            if (institutionId == Guid.Empty || studentId == Guid.Empty)
                return Result<InstitutionStudent>.Failure(EntityErrors.EmptyId);

            if (string.IsNullOrWhiteSpace(studentCode))
                return Result<InstitutionStudent>.Failure(EntityErrors.EmptyCode);

            var institutionStudent = new InstitutionStudent
            {
                Id = Guid.NewGuid(),
                InstitutionId = institutionId,
                StudentId = studentId,
                StudentCode = studentCode.Trim(),
                EnrolledAt = DateTime.UtcNow
            };

            return Result<InstitutionStudent>.Success(institutionStudent);
        }
    }
}
