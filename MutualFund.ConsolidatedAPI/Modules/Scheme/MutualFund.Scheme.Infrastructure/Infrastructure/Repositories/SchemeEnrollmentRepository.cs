using Microsoft.EntityFrameworkCore;
using MutualFund.Scheme.Domain.Entities;
using MutualFund.Scheme.Domain.Interfaces;
using MutualFund.Scheme.Infrastructure.Data;

namespace MutualFund.Scheme.Infrastructure.Repositories
{
    public class SchemeEnrollmentRepository : ISchemeEnrollmentRepository
    {
        private readonly ApplicationDbContext _context;

        public SchemeEnrollmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SchemeEnrollment>> GetAllAsync() =>
            await _context.SchemeEnrollments
                .AsNoTracking()
                .OrderBy(s => s.SchemeCode)
                .ToListAsync();

        public async Task<SchemeEnrollment?> GetBySchemeCodeAsync(string schemeCode) =>
            await _context.SchemeEnrollments
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SchemeCode == schemeCode);

        public async Task<IEnumerable<SchemeEnrollment>> GetApprovedSchemesAsync() =>
            await _context.SchemeEnrollments
                .AsNoTracking()
                .Where(s => s.IsApproved)
                .OrderBy(s => s.SchemeCode)
                .ToListAsync();

        public async Task<bool> ExistsBySchemeCodeAsync(string schemeCode) =>
            await _context.SchemeEnrollments
                .AnyAsync(s => s.SchemeCode == schemeCode);

        public async Task AddAsync(SchemeEnrollment scheme) =>
            await _context.SchemeEnrollments.AddAsync(scheme);

        public async Task UpdateAsync(string schemeCode, SchemeEnrollment updated)
        {
            var existing = await _context.SchemeEnrollments
                .FirstOrDefaultAsync(s => s.SchemeCode == schemeCode);

            if (existing is null) return;

            existing.SchemeName = updated.SchemeName;
            existing.IsApproved = updated.IsApproved;
            existing.UpdatedAt = updated.UpdatedAt;

            _context.SchemeEnrollments.Update(existing);
        }

        public async Task UpdateApprovalBySchemeCodesAsync(
            IEnumerable<string> schemeCodes, bool isApproved)
        {
            var schemeList = schemeCodes.ToList();
            if (schemeList.Count == 0) return;

            var schemes = await _context.SchemeEnrollments
                .Where(s => schemeList.Contains(s.SchemeCode))
                .ToListAsync();

            foreach (var scheme in schemes)
            {
                scheme.IsApproved = isApproved;
                scheme.UpdatedAt = DateTime.UtcNow;
            }

            _context.SchemeEnrollments.UpdateRange(schemes);
        }
    }
}