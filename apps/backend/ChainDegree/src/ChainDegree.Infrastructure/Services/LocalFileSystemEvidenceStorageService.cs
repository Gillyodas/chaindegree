using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.SharedKernel.DomainErrors.Reports;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Infrastructure.Services
{
    public class LocalFileSystemEvidenceStorageService : IEvidenceStorageService
    {
        private readonly string _storageDirectory;
        private readonly ILogger<LocalFileSystemEvidenceStorageService> _logger;

        public LocalFileSystemEvidenceStorageService(IHostEnvironment environment, ILogger<LocalFileSystemEvidenceStorageService> logger)
        {
            _logger = logger;
            // Store evidence files outside wwwroot in App_Data/Evidences
            _storageDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "Evidences");
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
        }

        public async Task<string> SaveEvidenceAsync(Stream contentStream, string contentType, string originalFileName, CancellationToken ct = default)
        {
            if (contentStream == null || contentStream.Length == 0)
            {
                throw new ArgumentException("Evidence stream is empty.", nameof(contentStream));
            }

            var extension = Path.GetExtension(originalFileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || (extension != ".pdf" && extension != ".png" && extension != ".jpg" && extension != ".jpeg"))
            {
                throw new InvalidOperationException(ReportErrors.InvalidEvidenceFormat.Message);
            }

            // Read magic numbers (first 8 bytes)
            var buffer = new byte[8];
            var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct);
            if (contentStream.CanSeek)
            {
                contentStream.Position = 0;
            }

            if (!ValidateMagicNumber(buffer, bytesRead, extension))
            {
                _logger.LogWarning("Evidence file failed magic number validation. Extension: {Extension}, ContentType: {ContentType}", extension, contentType);
                throw new InvalidOperationException(ReportErrors.InvalidEvidenceFormat.Message);
            }

            // Safe filename generation (Guid.NewGuid() + extension)
            var safeFileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(_storageDirectory, safeFileName);

            await using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await contentStream.CopyToAsync(fileStream, ct);
            }

            _logger.LogInformation("Saved evidence file safely to {SafeFileName}", safeFileName);
            return safeFileName;
        }

        public async Task<(Stream Stream, string ContentType, string DownloadFileName)?> GetEvidenceAsync(string fileName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            var safeName = Path.GetFileName(fileName);
            var fullPath = Path.Combine(_storageDirectory, safeName);

            if (!File.Exists(fullPath))
            {
                return null;
            }

            var extension = Path.GetExtension(safeName)?.ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            var memoryStream = new MemoryStream();
            await using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                await fileStream.CopyToAsync(memoryStream, ct);
            }

            memoryStream.Position = 0;
            return (memoryStream, contentType, $"evidence_{safeName}");
        }

        public Task DeleteEvidenceAsync(string fileName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Task.CompletedTask;
            }

            var safeName = Path.GetFileName(fileName);
            var fullPath = Path.Combine(_storageDirectory, safeName);

            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Deleted evidence file {FileName}", safeName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete evidence file {FileName}", safeName);
                }
            }

            return Task.CompletedTask;
        }

        private static bool ValidateMagicNumber(byte[] buffer, int bytesRead, string extension)
        {
            if (bytesRead < 4) return false;

            return extension switch
            {
                ".pdf" => buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46, // %PDF
                ".png" => buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47, // PNG header
                ".jpg" or ".jpeg" => buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF, // JPEG header
                _ => false
            };
        }
    }
}
