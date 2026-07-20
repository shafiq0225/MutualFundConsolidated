namespace MutualFund.Scheme.Domain.Helpers
{
    public static class FundCodeGenerator
    {
        public static string Generate(string fundName)
        {
            if (string.IsNullOrWhiteSpace(fundName))
                return string.Empty;

            const string suffix = "Mutual Fund";
            string result;

            if (fundName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                result = fundName
                    .Substring(0, fundName.Length - suffix.Length)
                    .Trim()
                    .Replace(" ", "");
                result += "_MF";
            }
            else
            {
                result = fundName.Replace(" ", "");
            }

            return result;
        }
    }
}