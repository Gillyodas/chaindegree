using System;

namespace ChainDegree.Core.Domain.SharedKernel.Interfaces;

public interface IInstitutionScoped
{
    Guid InstitutionId { get; }
}
