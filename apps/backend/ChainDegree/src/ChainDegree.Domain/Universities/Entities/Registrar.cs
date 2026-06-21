using System;
using System.Collections.Generic;
using System.Text;

namespace ChainDegree.Core.Domain.Universities.Entities
{
    public class Registrar
    {
        public Guid Id { get; private set; }
        public Guid InstitutionId { get; private set; }
        public Guid UserId { get; private set; } // Gắn logic sang AuthUser.Id bên Auth Module
        public string EmployeeCode { get; private set; } = null!;
        public string FullName { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
    }
}
