using System;

namespace ChainDegree.Core.Application.Degrees.Commands.UpdateDegree
{
    public sealed record UpdateDegreeResponse(
        Guid DegreeId,
        string Status,
        bool IsShortcut,
        string Message);
}
