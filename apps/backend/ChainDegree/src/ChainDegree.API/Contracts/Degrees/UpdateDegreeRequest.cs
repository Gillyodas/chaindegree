namespace ChainDegree.API.Contracts.Degrees
{
    public record UpdateDegreeRequest(
        string Major,
        string Classification,
        string ReasonCode);
}
