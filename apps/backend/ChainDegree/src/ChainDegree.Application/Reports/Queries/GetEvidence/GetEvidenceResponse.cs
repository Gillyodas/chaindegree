using System.IO;

namespace ChainDegree.Core.Application.Reports.Queries.GetEvidence
{
    public record GetEvidenceResponse(
        Stream Stream,
        string ContentType,
        string DownloadFileName
    );
}
