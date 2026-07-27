namespace ChainDegree.API.Contracts.Degrees
{
    public sealed record VerifyDegreeErrorResponse(
        bool Verified,
        string ErrorCode,
        string Message);
}
