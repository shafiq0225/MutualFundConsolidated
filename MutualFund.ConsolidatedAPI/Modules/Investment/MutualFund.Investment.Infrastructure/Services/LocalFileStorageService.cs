using MutualFund.Investment.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Infrastructure.Services
{
    /// <summary>
    /// Used in local development when Azure Blob Storage
    /// is not configured. Stores files in local folder.
    /// Switch to BlobStorageService in production.
    /// </summary>
    public class LocalFileStorageService : IBlobStorageService
    {
        private readonly string _basePath;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(
            IConfiguration configuration,
            ILogger<LocalFileStorageService> logger)
        {
            _logger = logger;
            _basePath = Path.Combine(
                AppContext.BaseDirectory,
                "LocalStorage",
                "Statements");

            Directory.CreateDirectory(_basePath);
        }

        public async Task<string> UploadAsync(
            Stream fileStream,
            string fileName,
            string contentType)
        {
            var folder = Guid.NewGuid().ToString();
            var dirPath = Path.Combine(_basePath, folder);
            Directory.CreateDirectory(dirPath);

            var filePath = Path.Combine(dirPath, fileName);

            using var fs = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(fs);

            _logger.LogInformation(
                "Saved file locally: {FilePath}", filePath);

            // Return a local "URL" that identifies the file
            return $"local://{folder}/{fileName}";
        }

        public Task<Stream> DownloadAsync(string blobUrl)
        {
            var parts = blobUrl.Replace("local://", "").Split('/');
            var filePath = Path.Combine(_basePath, parts[0], parts[1]);

            Stream stream = new FileStream(
                filePath, FileMode.Open, FileAccess.Read);
            return Task.FromResult(stream);
        }

        public Task DeleteAsync(string blobUrl)
        {
            var parts = blobUrl.Replace("local://", "").Split('/');
            var filePath = Path.Combine(_basePath, parts[0], parts[1]);
            if (File.Exists(filePath)) File.Delete(filePath);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string blobUrl)
        {
            var parts = blobUrl.Replace("local://", "").Split('/');
            var filePath = Path.Combine(_basePath, parts[0], parts[1]);
            return Task.FromResult(File.Exists(filePath));
        }
    }
}