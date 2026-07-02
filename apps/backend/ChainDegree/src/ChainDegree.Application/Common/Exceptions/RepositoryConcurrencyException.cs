using System;

namespace ChainDegree.Core.Application.Common.Exceptions
{
    public class RepositoryConcurrencyException : RepositoryException
    {
        public RepositoryConcurrencyException(string message) : base(message) { }
        public RepositoryConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
    }
}
