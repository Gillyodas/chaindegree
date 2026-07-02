using System;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.SharedKernel.Result;
using ChainDegree.SharedKernel.DomainErrors.Common;

namespace ChainDegree.Core.Domain.Students
{
    public class Student : AggregateRoot
    {
        public string IdentityNumber { get; private set; } = null!; // CCCD
        public string FullName { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public Guid UserId { get; private set; }

        private Student() { }

        public static Result<Student> Create(
            string identityNumber,
            string fullName,
            string email,
            Guid userId)
        {
            if (string.IsNullOrWhiteSpace(identityNumber))
                return Result<Student>.Failure(EntityErrors.EmptyCode);

            if (string.IsNullOrWhiteSpace(fullName))
                return Result<Student>.Failure(EntityErrors.EmptyName);

            if (string.IsNullOrWhiteSpace(email))
                return Result<Student>.Failure(EntityErrors.EmptyEmail);

            if (userId == Guid.Empty)
                return Result<Student>.Failure(EntityErrors.EmptyId);

            var student = new Student
            {
                Id = Guid.NewGuid(),
                IdentityNumber = identityNumber.Trim(),
                FullName = fullName.Trim(),
                Email = email.Trim().ToLower(),
                UserId = userId
            };

            return Result<Student>.Success(student);
        }
    }
}
