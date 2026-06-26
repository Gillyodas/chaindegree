using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Universities.Entities;

namespace ChainDegree.Core.Domain.Universities
{
    public class EducationInstitution
    {
        public Guid Id { get; private set; }
        public string Code { get; private set; } = null!;
        public string Name { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private readonly List<Registrar> _registrars = new();
        public IReadOnlyCollection<Registrar> Registrars => _registrars.AsReadOnly();
    }
}
