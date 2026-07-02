using ChainDegree.Core.Application.Abstractions.Auth;

namespace ChainDegree.Core.Infrastructure.Auth
{
    public class FakeRoleChecker : IRoleChecker
    {
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public FakeRoleChecker(ICurrentUserAccessor currentUserAccessor)
        {
            _currentUserAccessor = currentUserAccessor;
        }

        public bool IsInRole(string role)
        {
            if (!_currentUserAccessor.IsAuthenticated) return false;
            return string.Equals(_currentUserAccessor.Role, role, System.StringComparison.OrdinalIgnoreCase);
        }

        public bool HasPermission(string permission)
        {
            if (!_currentUserAccessor.IsAuthenticated) return false;

            var role = _currentUserAccessor.Role;

            if (string.Equals(role, Roles.Admin, System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(role, Roles.Registrar, System.StringComparison.OrdinalIgnoreCase))
            {
                return permission.StartsWith("degree:") || permission.StartsWith("institution:");
            }

            if (string.Equals(role, Roles.Recruiter, System.StringComparison.OrdinalIgnoreCase))
            {
                return permission.StartsWith("job:") || permission.StartsWith("recruitment:");
            }

            return false;
        }
    }
}
