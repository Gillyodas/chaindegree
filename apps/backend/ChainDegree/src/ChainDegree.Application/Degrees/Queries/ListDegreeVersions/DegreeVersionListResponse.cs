using System;
using System.Collections.Generic;

namespace ChainDegree.Core.Application.Degrees.Queries.ListDegreeVersions
{
    public sealed record DegreeVersionListResponse(
        string DegreeCode,
        int CurrentVersion,
        IReadOnlyList<DegreeVersionItem> Versions);

    public sealed record DegreeVersionItem(
        int Version,
        DateTime EffectiveAt,
        bool IsCurrent);
}
