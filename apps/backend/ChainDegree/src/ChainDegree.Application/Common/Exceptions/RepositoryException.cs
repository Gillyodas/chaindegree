using System;

namespace ChainDegree.Core.Application.Common.Exceptions
{
    public class RepositoryException : Exception, IProblemException
    {
        public virtual int StatusCode => 400;
        public virtual string ErrorCode => "REPOSITORY_ERROR";
        public virtual string Detail => Message;

        public RepositoryException(string message) : base(message) { }
        public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
    }
}
