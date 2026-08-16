using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Queries.ListDegreeVersions
{
    public sealed record ListDegreeVersionsQuery(string DegreeCode) : IRequest<Result<DegreeVersionListResponse>>;
}
