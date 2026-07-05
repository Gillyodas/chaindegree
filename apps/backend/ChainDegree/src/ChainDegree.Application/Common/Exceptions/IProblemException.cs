namespace ChainDegree.Core.Application.Common.Exceptions
{
    public interface IProblemException
    {
        int StatusCode { get; }
        string ErrorCode { get; }
        string Detail { get; }
    }
}
