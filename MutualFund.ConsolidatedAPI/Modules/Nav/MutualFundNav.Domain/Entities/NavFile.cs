namespace MutualFundNav.Domain.Entities
{
    public class NavFile : BaseEntity
    {
        public DateTime NavDate { get; set; }
        public string FileContent { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int RecordCount { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public DateTime DownloadedAt { get; set; }
        public bool IsHoliday { get; set; }
    }
}
