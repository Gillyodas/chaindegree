using System;
using System.Collections.Generic;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.Core.Domain.Universities.Entities;

namespace ChainDegree.Core.Domain.Universities
{
    public class EducationInstitution : Entity
    {
        public string Code { get; private set; } = null!;
        public string Name { get; private set; } = null!;
        public string Email { get; private set; } = null!;

        private readonly List<Registrar> _registrars = new();
        public IReadOnlyCollection<Registrar> Registrars => _registrars.AsReadOnly();

        private readonly List<DegreeType> _degreeTypes = new();
        public IReadOnlyCollection<DegreeType> DegreeTypes => _degreeTypes.AsReadOnly();

        private EducationInstitution() { }

        public static EducationInstitution Create(string code, string name, string email)
        {
            return new EducationInstitution
            {
                Id = Guid.NewGuid(),
                Code = code.Trim(),
                Name = name.Trim(),
                Email = email.Trim().ToLower()
            };
        }
    }
}
