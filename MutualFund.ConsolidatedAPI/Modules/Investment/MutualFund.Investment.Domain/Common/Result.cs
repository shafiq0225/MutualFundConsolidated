namespace MutualFund.Investment.Domain.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? ErrorMessage { get; private set; }

        private Result() { }

        public static Result<T> Success(T data) =>
            new() { IsSuccess = true, Data = data };

        public static Result<T> Failure(string error) =>
            new() { IsSuccess = false, ErrorMessage = error };
    }

    // Non-generic version for commands that return no data
    public class Result
    {
        public bool IsSuccess { get; private set; }
        public string? ErrorMessage { get; private set; }

        private Result() { }

        public static Result Success() =>
            new() { IsSuccess = true };

        public static Result Failure(string error) =>
            new() { IsSuccess = false, ErrorMessage = error };
    }
}