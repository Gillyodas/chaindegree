namespace ChainDegree.SharedKernel.Common.Error
{
    public enum ErrorType
    {
        Failure = 0,
        Validation = 1, // 400
        NotFound = 2,   // 404
        Conflict = 3,   // 409
        Unauthorized = 4, // 401
        Forbidden = 5     // 403
    }

    public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
    {
        public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

        // --- S?A L?I: Factory Method ph?i truy?n dúng ErrorType ---

        // Validation (400)
        public static Error Validation(string code, string message) =>
            new(code, message, ErrorType.Validation);

        // Conflict (409)
        public static Error Conflict(string code, string message) =>
            new(code, message, ErrorType.Conflict);

        // Not Found (404)
        public static Error NotFound(string code, string message) =>
            new(code, message, ErrorType.NotFound);

        // Unauthorized (401)
        public static Error Unauthorized(string code, string message) =>
            new(code, message, ErrorType.Unauthorized);

        // Forbidden (403)
        public static Error Forbidden(string code, string message) =>
            new(code, message, ErrorType.Forbidden);

        // Failure (500 ho?c 400 chung)
        public static Error Failure(string code, string message) =>
            new(code, message, ErrorType.Failure);

        // Unexpected (500)
        public static Error Unexpected(string code, string message) =>
            new(code, message, ErrorType.Failure);
    }
}
