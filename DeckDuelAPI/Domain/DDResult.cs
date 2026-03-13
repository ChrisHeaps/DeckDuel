namespace DeckDuel2.Domain
{

    public enum DDError
    {
        None,
        AlreadyExists,
        InvalidInput,
        Unauthorized,
        NotOwner,
        NotFound
    }

    public class DDResult<T>
    {

        public bool Success { get; init; }
        public DDError ErrorType { get; init; }
        public string? Error { get; init; }
        public T? Value { get; init; }

        public static DDResult<T> Ok(T value) =>
            new() { Success = true, Value = value, ErrorType = DDError.None };

        public static DDResult<T> Fail(DDError errorType, string error) =>
            new() { Success = false, ErrorType = errorType, Error = error };
    }
}
