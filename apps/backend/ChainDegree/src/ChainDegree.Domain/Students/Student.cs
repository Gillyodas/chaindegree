using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Applications;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Reports;

namespace ChainDegree.Core.Domain.Students
{
    public class Student
    {
        public Guid Id { get; private set; }
        public string StudentCode { get; private set; } = null!;
        public string FullName { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public Guid UserId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private readonly List<Degree> _degrees = new();
        public IReadOnlyCollection<Degree> Degrees => _degrees.AsReadOnly();

        private readonly List<Report> _reports = new();
        public IReadOnlyCollection<Report> Reports => _reports.AsReadOnly();

        private readonly List<Application> _applications = new();
        public IReadOnlyCollection<Application> Applications => _applications.AsReadOnly();
    }
}
