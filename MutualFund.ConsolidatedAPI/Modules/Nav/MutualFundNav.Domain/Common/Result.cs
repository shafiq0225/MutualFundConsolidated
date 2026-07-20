namespace MutualFundNav.Domain.Common
{
    public sealed class Result<T>
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
}
