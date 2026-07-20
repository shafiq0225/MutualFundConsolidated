namespace MutualFund.Scheme.Domain.Exceptions
{
    public abstract class SchemeApiException : Exception
    {
        public string ErrorCode { get; }
        public int StatusCode { get; }

        protected SchemeApiException(string message, string errorCode, int statusCode)
            : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }

    public class ValidationException : SchemeApiException
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; }

        public ValidationException(Dictionary<string, string[]> errors)
            : base("One or more validation errors occurred.", "VALIDATION_ERROR", 400)
        {
            Errors = errors;
        }
    }

    public class NotFoundException : SchemeApiException
    {
        public NotFoundException(string entity, string key)
            : base($"{entity} '{key}' was not found.", "NOT_FOUND", 404) { }
    }

    public class NavDataNotFoundException : SchemeApiException
    {
        public NavDataNotFoundException(DateTime start, DateTime end)
            : base($"No NAV data found between {start:yyyy-MM-dd} and {end:yyyy-MM-dd}.",
                  "NAV_DATA_NOT_FOUND", 404)
        { }
    }

    public class DuplicateException : SchemeApiException
    {
        public DuplicateException(string entity, string key)
            : base($"{entity} '{key}' already exists.", "DUPLICATE", 409) { }
    }

    public class SchemeEnrollmentException : SchemeApiException
    {
        public SchemeEnrollmentException(string message)
            : base(message, "SCHEME_ENROLLMENT_ERROR", 400) { }
    }

    public class FundApprovalException : SchemeApiException
    {
        public FundApprovalException(string message, string fundCode)
            : base(message, "FUND_APPROVAL_ERROR", 400) { }
    }

    public class NavConsumerException : Exception
    {
        public DateTime NavDate { get; }

        public NavConsumerException(string message, DateTime navDate, Exception inner)
            : base(message, inner)
        {
            NavDate = navDate;
        }
    }
}