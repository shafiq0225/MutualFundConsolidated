using MutualFund.Scheme.Application.DTOs;
using MutualFund.Scheme.Domain.Entities;
using MutualFund.Scheme.Domain.Exceptions;
using MutualFund.Scheme.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Scheme.Application.UseCases.Commands
{
    public class UpdateSchemeEnrollmentCommand
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateSchemeEnrollmentCommand> _logger;

        public UpdateSchemeEnrollmentCommand(IUnitOfWork unitOfWork,
            ILogger<UpdateSchemeEnrollmentCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<SchemeEnrollmentDto> ExecuteAsync(
            string schemeCode, UpdateSchemeEnrollmentDto dto)
        {
            var existing = await _unitOfWork.SchemeEnrollments
                .GetBySchemeCodeAsync(schemeCode);

            if (existing is null)
                throw new NotFoundException("SchemeEnrollment", schemeCode);

            existing.SchemeName = dto.SchemeName.Trim();
            existing.FundName = dto.FundName.Trim();
            existing.IsApproved = dto.IsApproved;
            existing.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SchemeEnrollments.UpdateAsync(schemeCode, existing);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation(
                "SchemeEnrollment updated — SchemeCode={Code}", schemeCode);

            return MapToDto(existing);
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