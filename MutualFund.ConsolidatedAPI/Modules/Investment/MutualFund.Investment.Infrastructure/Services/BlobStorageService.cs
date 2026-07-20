using MutualFund.Investment.Domain.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Infrastructure.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly ILogger<BlobStorageService> _logger;

        public BlobStorageService(
            IConfiguration configuration,
            ILogger<BlobStorageService> logger)
        {
            _logger = logger;
            _containerName = configuration["AzureStorage:ContainerName"]
                             ?? "investment-statements";

            var connectionString =
                configuration["AzureStorage:ConnectionString"];

            // ── Local development fallback ─────────────────────────
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Use local file storage in development
                // when Azure Blob is not configured
                _blobServiceClient = new BlobServiceClient(
                    "UseDevelopmentStorage=true");
                _logger.LogWarning(
                    "Azure Blob Storage not configured. " +
                    "Using local development storage.");
            }
            else
            {
                _blobServiceClient = new BlobServiceClient(connectionString);
            }
        }

        // ── Upload ────────────────────────────────────────────────
        public async Task<string> UploadAsync(
            Stream fileStream,
            string fileName,
            string contentType)
        {
            try
            {
                var containerClient = _blobServiceClient
                    .GetBlobContainerClient(_containerName);

                // Create container if not exists
                await containerClient.CreateIfNotExistsAsync(
                    PublicAccessType.None);

                // Generate unique blob name
                var blobName = $"{Guid.NewGuid()}/{fileName}";

                var blobClient = containerClient.GetBlobClient(blobName);

                var uploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                };

                await blobClient.UploadAsync(fileStream, uploadOptions);

                _logger.LogInformation(
                    "Uploaded blob: {BlobName}", blobName);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to upload blob: {FileName}", fileName);
                throw;
            }
        }

        // ── Download ──────────────────────────────────────────────
        public async Task<Stream> DownloadAsync(string blobUrl)
        {
            try
            {
                var blobClient = new BlobClient(new Uri(blobUrl));
                var response = await blobClient.DownloadAsync();
                return response.Value.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to download blob: {BlobUrl}", blobUrl);
                throw;
            }
        }

        // ── Delete ────────────────────────────────────────────────
        public async Task DeleteAsync(string blobUrl)
        {
            try
            {
                var blobClient = new BlobClient(new Uri(blobUrl));
                await blobClient.DeleteIfExistsAsync();

                _logger.LogInformation(
                    "Deleted blob: {BlobUrl}", blobUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to delete blob: {BlobUrl}", blobUrl);
                throw;
            }
        }

        // ── Exists ────────────────────────────────────────────────
        public async Task<bool> ExistsAsync(string blobUrl)
        {
            try
            {
                var blobClient = new BlobClient(new Uri(blobUrl));
                var response = await blobClient.ExistsAsync();
                return response.Value;
            }
            catch
            {
                return false;
            }
        }
    }
}