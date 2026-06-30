using System;

namespace ChainDegree.Core.Domain.Auth
{
    public class AuthUser
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; } = null!;
        public string PasswordHash { get; private set; } = null!;
        public bool IsActive { get; private set; }

        private AuthUser() { }

        public AuthUser(Guid id, string email, string passwordHash, bool isActive)
        {
            Id = id;
            Email = email;
            PasswordHash = passwordHash;
            IsActive = isActive;
        }
    }
}
