using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.SharedKernel.Common.Error;

namespace ChainDegree.SharedKernel.DomainErrors.Jobs
{
    public class JobErrors
    {
        public static readonly Error EmptyIdentifiers =
            Error.Validation("Job.EmptyIdentifiers", "Company ID and Created By Agent ID cannot be empty.");

        public static readonly Error MissingJobDetails =
            Error.Validation("Job.MissingJobDetails", "Job title and description are required and cannot be empty.");

        public static readonly Error InvalidSalaryRange =
            Error.Validation("Job.InvalidSalaryRange", "Minimum salary cannot be negative, and maximum salary must be greater than or equal to minimum salary.");

        public static readonly Error InvalidDateRange =
            Error.Validation("Job.InvalidDateRange", "The application start date must be before the end date.");

        public static readonly Error EndDateInPast =
            Error.Validation("Job.EndDateInPast", "The application end date must be in the future.");
    }
}
