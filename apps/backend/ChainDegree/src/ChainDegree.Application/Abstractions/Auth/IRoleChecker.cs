namespace ChainDegree.Core.Application.Abstractions.Auth
{
    public interface IRoleChecker
    {
        bool IsInRole(string role);
        bool HasPermission(string permission);
    }
}
