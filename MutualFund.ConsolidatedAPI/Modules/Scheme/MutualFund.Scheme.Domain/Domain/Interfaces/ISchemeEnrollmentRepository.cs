using MutualFund.Scheme.Domain.Entities;

namespace MutualFund.Scheme.Domain.Interfaces
{
    public interface ISchemeEnrollmentRepository
    {
        Task<IEnumerable<SchemeEnrollment>> GetAllAsync();
        Task<SchemeEnrollment?> GetBySchemeCodeAsync(string schemeCode);
        Task<IEnumerable<SchemeEnrollment>> GetApprovedSchemesAsync();
        Task<bool> ExistsBySchemeCodeAsync(string schemeCode);
        Task AddAsync(SchemeEnrollment scheme);
        Task UpdateAsync(string schemeCode, SchemeEnrollment scheme);
        Task UpdateApprovalBySchemeCodesAsync(IEnumerable<string> schemeCodes, bool isApproved);
    }
}