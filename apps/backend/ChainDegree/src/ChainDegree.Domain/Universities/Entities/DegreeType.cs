using System;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;
using ChainDegree.SharedKernel.Result;
using ChainDegree.SharedKernel.DomainErrors.Common;

namespace ChainDegree.Core.Domain.Universities.Entities
{
    public class DegreeType : Entity, IInstitutionScoped
    {
        public Guid InstitutionId { get; private set; }
        public string Code { get; private set; } = null!;
        public string Name { get; private set; } = null!;

        private DegreeType() { }

        public static Result<DegreeType> Create(
            Guid institutionId,
            string code,
            string name)
        {
            if (institutionId == Guid.Empty)
                return Result<DegreeType>.Failure(EntityErrors.EmptyId);

            if (string.IsNullOrWhiteSpace(code))
                return Result<DegreeType>.Failure(EntityErrors.EmptyCode);

            if (string.IsNullOrWhiteSpace(name))
                return Result<DegreeType>.Failure(EntityErrors.EmptyName);

            var degreeType = new DegreeType
            {
                Id = Guid.NewGuid(),
                InstitutionId = institutionId,
                Code = code.Trim(),
                Name = name.Trim()
            };

            return Result<DegreeType>.Success(degreeType);
        }
    }
}
