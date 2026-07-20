namespace MutualFund.Auth.Domain.Exceptions
{
    /// <summary>Base exception for all Auth API errors.</summary>
    public class AuthException : Exception
    {
        public string ErrorCode { get; }
        public int StatusCode { get; }

        public AuthException(string message,
            string errorCode = "AUTH_ERROR",
            int statusCode = 400)
            : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }

        public AuthException(string message, Exception innerException,
            string errorCode = "AUTH_ERROR",
            int statusCode = 400)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }

    /// <summary>Invalid email or password on login.</summary>
    public class InvalidCredentialsException : AuthException
    {
        public InvalidCredentialsException()
            : base("Invalid email or password.",
                  "INVALID_CREDENTIALS", 401)
        { }
    }

    /// <summary>Account registered but not yet approved by Admin.</summary>
    public class AccountPendingApprovalException : AuthException
    {
        public AccountPendingApprovalException()
            : base("Your account is pending admin approval. " +
                  "You will be notified once approved.",
                  "ACCOUNT_PENDING", 403)
        { }
    }

    /// <summary>Account was rejected by Admin.</summary>
    public class AccountRejectedException : AuthException
    {
        public string? Reason { get; }

        public AccountRejectedException(string? reason = null)
            : base(string.IsNullOrWhiteSpace(reason)
                  ? "Your account registration has been rejected."
                  : $"Your account has been rejected. Reason: {reason}",
                  "ACCOUNT_REJECTED", 403)
        {
            Reason = reason;
        }
    }

    /// <summary>Account is deactivated by Admin.</summary>
    public class AccountDisabledException : AuthException
    {
        public AccountDisabledException()
            : base("Your account has been deactivated. " +
                  "Please contact the administrator.",
                  "ACCOUNT_DISABLED", 403)
        { }
    }

    /// <summary>Email already registered in the system.</summary>
    public class EmailAlreadyExistsException : AuthException
    {
        public EmailAlreadyExistsException(string email)
            : base($"The email '{email}' is already registered.",
                  "EMAIL_EXISTS", 409)
        { }
    }

    /// <summary>PAN number already registered in the system.</summary>
    public class PanAlreadyExistsException : AuthException
    {
        public PanAlreadyExistsException(string panNumber)
            : base($"The PAN number '{panNumber}' is already registered.",
                  "PAN_EXISTS", 409)
        { }
    }

    /// <summary>Invalid PAN number format.</summary>
    public class InvalidPanFormatException : AuthException
    {
        public InvalidPanFormatException(string panNumber)
            : base($"'{panNumber}' is not a valid PAN number. " +
                  "Expected format: 5 letters + 4 digits + 1 letter (e.g. ABCDE1234F).",
                  "INVALID_PAN_FORMAT", 400)
        { }
    }

    /// <summary>JWT refresh token is invalid, expired or revoked.</summary>
    public class InvalidRefreshTokenException : AuthException
    {
        public InvalidRefreshTokenException()
            : base("Refresh token is invalid, expired or has been revoked.",
                  "INVALID_REFRESH_TOKEN", 401)
        { }
    }

    /// <summary>User not found by id or email.</summary>
    public class UserNotFoundException : AuthException
    {
        public UserNotFoundException(string identifier)
            : base($"User '{identifier}' was not found.",
                  "USER_NOT_FOUND", 404)
        { }
    }

    /// <summary>Permission code not found in master list.</summary>
    public class PermissionNotFoundException : AuthException
    {
        public PermissionNotFoundException(string code)
            : base($"Permission '{code}' was not found.",
                  "PERMISSION_NOT_FOUND", 404)
        { }
    }

    /// <summary>Permission already assigned to this user.</summary>
    public class PermissionAlreadyAssignedException : AuthException
    {
        public PermissionAlreadyAssignedException(
            string permission, string userId)
            : base($"Permission '{permission}' is already " +
                  $"assigned to user '{userId}'.",
                  "PERMISSION_ALREADY_ASSIGNED", 409)
        { }
    }

    /// <summary>Permission not assigned — cannot revoke.</summary>
    public class PermissionNotAssignedException : AuthException
    {
        public PermissionNotAssignedException(
            string permission, string userId)
            : base($"Permission '{permission}' is not " +
                  $"currently assigned to user '{userId}'.",
                  "PERMISSION_NOT_ASSIGNED", 400)
        { }
    }

    /// <summary>Caller not authorized to perform this action.</summary>
    public class UnauthorizedActionException : AuthException
    {
        public UnauthorizedActionException(string action)
            : base($"You are not authorized to perform '{action}'.",
                  "UNAUTHORIZED_ACTION", 403)
        { }
    }

    /// <summary>Family group not found.</summary>
    public class FamilyGroupNotFoundException : AuthException
    {
        public FamilyGroupNotFoundException(int groupId)
            : base($"Family group with Id '{groupId}' was not found.",
                  "FAMILY_GROUP_NOT_FOUND", 404)
        { }
    }

    /// <summary>User is already a member of a family group.</summary>
    public class UserAlreadyInFamilyException : AuthException
    {
        public UserAlreadyInFamilyException(string userId)
            : base($"User '{userId}' is already a member of a family group.",
                  "USER_ALREADY_IN_FAMILY", 409)
        { }
    }

    /// <summary>User is not a member of the specified family group.</summary>
    public class UserNotInFamilyException : AuthException
    {
        public UserNotInFamilyException(string userId, int groupId)
            : base($"User '{userId}' is not a member of " +
                  $"family group '{groupId}'.",
                  "USER_NOT_IN_FAMILY", 400)
        { }
    }

    /// <summary>Cannot change password — current password is wrong.</summary>
    public class IncorrectCurrentPasswordException : AuthException
    {
        public IncorrectCurrentPasswordException()
            : base("Current password is incorrect.",
                  "INCORRECT_CURRENT_PASSWORD", 400)
        { }
    }

    /// <summary>Identity operation failed (e.g. password too weak).</summary>
    public class IdentityOperationException : AuthException
    {
        public IEnumerable<string> Errors { get; }

        public IdentityOperationException(IEnumerable<string> errors)
            : base(string.Join(", ", errors),
                  "IDENTITY_OPERATION_FAILED", 400)
        {
            Errors = errors;
        }
    }
}