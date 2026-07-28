using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ChainDegree.Core.Application.Abstractions.Services
{
    public interface IEvidenceStorageService
    {
        Task<string> SaveEvidenceAsync(Stream contentStream, string contentType, string originalFileName, CancellationToken ct = default);
        Task<(Stream Stream, string ContentType, string DownloadFileName)?> GetEvidenceAsync(string fileName, CancellationToken ct = default);
        Task DeleteEvidenceAsync(string fileName, CancellationToken ct = default);
    }
}
