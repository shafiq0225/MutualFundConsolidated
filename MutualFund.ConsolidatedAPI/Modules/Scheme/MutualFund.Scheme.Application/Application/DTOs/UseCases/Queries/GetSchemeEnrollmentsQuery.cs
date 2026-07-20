using MutualFund.Scheme.Application.DTOs;
using MutualFund.Scheme.Domain.Entities;
using MutualFund.Scheme.Domain.Exceptions;
using MutualFund.Scheme.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Scheme.Application.UseCases.Queries
{
    public class GetSchemeEnrollmentsQuery
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetSchemeEnrollmentsQuery> _logger;

        public GetSchemeEnrollmentsQuery(IUnitOfWork unitOfWork,
            ILogger<GetSchemeEnrollmentsQuery> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<SchemeEnrollmentDto>> GetAllAsync()
        {
            var list = await _unitOfWork.SchemeEnrollments.GetAllAsync();
            return list.Select(MapToDto);
        }

        public async Task<SchemeEnrollmentDto> GetBySchemeCodeAsync(string schemeCode)
        {
            var entity = await _unitOfWork.SchemeEnrollments
                .GetBySchemeCodeAsync(schemeCode);

            if (entity is null)
                throw new NotFoundException("SchemeEnrollment", schemeCode);

            return MapToDto(entity);
        }

        public async Task<IEnumerable<SchemeEnrollmentDto>> GetApprovedAsync()
        {
            var list = await _unitOfWork.SchemeEnrollments.GetApprovedSchemesAsync();
            return list.Select(MapToDto);
        }

        private static SchemeEnrollmentDto MapToDto(SchemeEnrollment e) => new()
        {
            Id = e.Id,
            SchemeCode = e.SchemeCode,
            SchemeName = e.SchemeName,
            FundName = e.FundName,
            IsApproved = e.IsApproved,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}