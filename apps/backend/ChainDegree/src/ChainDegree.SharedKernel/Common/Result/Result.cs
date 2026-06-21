using ChainDegree.SharedKernel.Common.Error;

namespace ChainDegree.SharedKernel.Result
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Error Error { get; }
        public Exception? Exception { get; }

        protected Result(bool isSuccess, Error error, Exception? exception = null)
        {
            IsSuccess = isSuccess;
            Error = error;
            Exception = exception;
        }

        public static Result Success() =>
            new(true, Error.None);

        public static Result Failure(Error error, Exception? ex = null) =>
            new(false, error, ex);
    }

    public class Result<T> : Result
    {
        public T Value { get; }

        private Result(T value, bool isSuccess, Error error, Exception? exception = null)
            : base(isSuccess, error, exception)
        {
            Value = value;
        }

        public static Result<T> Success(T value) =>
            new(value, true, Error.None);

        public static Result<T> Failure(Error error, Exception? ex = null) =>
            new(default!, false, error, ex);
    }

    /// <summary>
    /// K?t qu? d?ng partial cho batch operation (m?t ph?n thành công, m?t ph?n th?t b?i)
    /// </summary>
    public class PartialResult<TSuccess, TFailure>
    {
        public IReadOnlyList<TSuccess> Successes { get; }
        public IReadOnlyList<TFailure> Failures { get; }

        public bool IsFullSuccess => Failures.Count == 0 && Successes.Count > 0;
        public bool IsFullFailure => Successes.Count == 0 && Failures.Count > 0;
        public bool IsPartial => Successes.Count > 0 && Failures.Count > 0;

        private PartialResult(IEnumerable<TSuccess> successes, IEnumerable<TFailure> failures)
        {
            Successes = successes.ToList();
            Failures = failures.ToList();
        }

        public static PartialResult<TSuccess, TFailure> Create(
            IEnumerable<TSuccess> successes,
            IEnumerable<TFailure> failures) =>
            new(successes, failures);

        public static PartialResult<TSuccess, TFailure> FromSuccess(IEnumerable<TSuccess> successes) =>
            new(successes, Array.Empty<TFailure>());

        public static PartialResult<TSuccess, TFailure> FromFailure(IEnumerable<TFailure> failures) =>
            new(Array.Empty<TSuccess>(), failures);
    }

    public class Maybe<T>
    {
        private readonly T? _value;

        public bool HasValue { get; }
        public bool HasNoValue => !HasValue;
        public T Value => HasValue
            ? _value!
            : throw new InvalidOperationException("No value present");

        private Maybe() => HasValue = false;

        private Maybe(T value)
        {
            _value = value;
            HasValue = true;
        }

        public static Maybe<T> None => new Maybe<T>();

        public static Maybe<T> From(T value) =>
            value == null ? None : new Maybe<T>(value);

        public TResult Match<TResult>(
            Func<T, TResult> some,
            Func<TResult> none) =>
            HasValue ? some(_value!) : none();

        public void Match(Action<T> some, Action none)
        {
            if (HasValue) some(_value!);
            else none();
        }

        public T GetValueOrDefault(T defaultValue = default!) =>
            HasValue ? _value! : defaultValue;
    }
}
