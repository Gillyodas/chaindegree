using System;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;

namespace ChainDegree.Core.Domain.Universities.Entities
{
    public class Registrar : Entity, IInstitutionScoped
    {
        public Guid InstitutionId { get; private set; }
        public Guid UserId { get; private set; }
        public string EmployeeCode { get; private set; } = null!;
        public string FullName { get; private set; } = null!;

        private Registrar() { }

        public static Registrar Create(Guid institutionId, Guid userId, string employeeCode, string fullName)
        {
            return new Registrar
            {
                Id = Guid.NewGuid(),
                InstitutionId = institutionId,
                UserId = userId,
                EmployeeCode = employeeCode.Trim(),
                FullName = fullName.Trim()
            };
        }
    }
}
