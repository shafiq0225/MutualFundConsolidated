namespace MutualFund.Investment.Domain.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadAsync(
            Stream fileStream,
            string fileName,
            string contentType);
        // Returns: full blob URL

        Task<Stream> DownloadAsync(string blobUrl);

        Task DeleteAsync(string blobUrl);

        Task<bool> ExistsAsync(string blobUrl);
    }
}