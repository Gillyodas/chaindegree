using System;

namespace ChainDegree.Core.Domain.SharedKernel.Interfaces;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
    Guid CreatedBy { get; }
    Guid UpdatedBy { get; }
}
