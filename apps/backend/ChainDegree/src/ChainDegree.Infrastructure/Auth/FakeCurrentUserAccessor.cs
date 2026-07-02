using System;
using ChainDegree.Core.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Http;

namespace ChainDegree.Core.Infrastructure.Auth
{
    public class FakeCurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FakeCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid UserId
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context?.Request.Headers.TryGetValue("X-User-Id", out var values) == true &&
                    Guid.TryParse(values.ToString(), out var userId))
                {
                    return userId;
                }
                // Default fallback for dev/testing
                return Guid.Parse("00000000-0000-0000-0000-000000000001");
            }
        }

        public Guid? InstitutionId
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context?.Request.Headers.TryGetValue("X-Institution-Id", out var values) == true &&
                    Guid.TryParse(values.ToString(), out var instId))
                {
                    return instId;
                }
                // Default fallback for dev/testing
                return Guid.Parse("11111111-1111-1111-1111-111111111111");
            }
        }

        public string Role
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context?.Request.Headers.TryGetValue("X-Role", out var values) == true)
                {
                    return values.ToString();
                }
                // Default fallback for dev/testing
                return Roles.Registrar;
            }
        }

        public string IpAddress
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                return context?.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            }
        }

        public bool IsAuthenticated => true;
    }
}
