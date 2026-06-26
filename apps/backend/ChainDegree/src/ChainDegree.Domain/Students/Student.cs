using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Applications;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Reports;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.Core.Domain.Students.Events;

namespace ChainDegree.Core.Domain.Students
{
    public class Student : Entity
    {
        public Guid Id { get; private set; }
        public string StudentCode { get; private set; } = null!;
        public string FullName { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public Guid UserId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
    }
}
