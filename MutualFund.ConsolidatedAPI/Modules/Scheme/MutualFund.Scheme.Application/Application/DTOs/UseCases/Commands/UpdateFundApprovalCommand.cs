using MutualFund.Scheme.Domain.Exceptions;
using MutualFund.Scheme.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Scheme.Application.UseCases.Commands
{
    public class UpdateFundApprovalCommand
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateFundApprovalCommand> _logger;

        public UpdateFundApprovalCommand(IUnitOfWork unitOfWork,
            ILogger<UpdateFundApprovalCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> ExecuteAsync(string fundCode, bool isApproved)
        {
            var schemeCodes = await _unitOfWork.DetailedSchemes
                .GetSchemeCodesByFundCodeAsync(fundCode);

            var schemeCodeList = schemeCodes.ToList();

            if (schemeCodeList.Count == 0)
                throw new FundApprovalException(
                    $"No schemes found for FundCode '{fundCode}'.", fundCode);

            await _unitOfWork.DetailedSchemes
                .UpdateApprovalByFundCodeAsync(fundCode, isApproved);

            await _unitOfWork.SchemeEnrollments
                .UpdateApprovalBySchemeCodesAsync(schemeCodeList, isApproved);

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation(
                "Fund approval updated — FundCode={FundCode} IsApproved={IsApproved} SchemesAffected={Count}",
                fundCode, isApproved, schemeCodeList.Count);

            return schemeCodeList.Count;
        }
    }
}