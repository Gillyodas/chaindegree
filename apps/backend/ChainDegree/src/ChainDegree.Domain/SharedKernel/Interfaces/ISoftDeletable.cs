using System;

namespace ChainDegree.Core.Domain.SharedKernel.Interfaces;

public interface ISoftDeletable
{
    DateTime? DeletedAt { get; }
    bool IsDeleted { get; }
}
