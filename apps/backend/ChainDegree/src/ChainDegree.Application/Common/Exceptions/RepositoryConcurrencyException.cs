using System;

namespace ChainDegree.Core.Application.Common.Exceptions
{
    public class RepositoryConcurrencyException : RepositoryException
    {
        public override int StatusCode => 409;
        public override string ErrorCode => "CONCURRENCY_ERROR";

        public RepositoryConcurrencyException(string message) : base(message) { }
        public RepositoryConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
    }
}
