using System;

namespace ChainDegree.Core.Application.Abstractions.Auth
{
    public interface ICurrentUserAccessor
    {
        Guid UserId { get; }
        Guid? InstitutionId { get; }
        string Role { get; }
        string IpAddress { get; }
        bool IsAuthenticated { get; }
    }
}
