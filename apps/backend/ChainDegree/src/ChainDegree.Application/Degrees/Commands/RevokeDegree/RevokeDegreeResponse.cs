using System;

namespace ChainDegree.Core.Application.Degrees.Commands.RevokeDegree
{
    public sealed record RevokeDegreeResponse(
        Guid DegreeId,
        string Status,
        bool IsShortcut,
        string Message);
}
