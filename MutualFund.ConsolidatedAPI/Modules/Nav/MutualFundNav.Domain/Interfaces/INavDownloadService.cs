using MutualFundNav.Domain.Enums;

namespace MutualFundNav.Domain.Interfaces
{
    public interface INavDownloadService
    {
        Task<(DownloadStatus Status, string Content, string? ErrorMessage, int RecordCount)> DownloadNavDataAsync();
    }
}
