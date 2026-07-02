using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.Core.Infrastructure.Persistence;

namespace ChainDegree.Core.Infrastructure.Services
{
    public class BehaviorLogService : IBehaviorLogService
    {
        private readonly ChainDegreeDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public BehaviorLogService(ChainDegreeDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task LogAsync(
            ActionTypeEnum actionType,
            string targetTable,
            Guid targetId,
            string? oldValuesJson,
            string newValuesJson,
            CancellationToken ct = default)
        {
            var actorRole = _currentUserAccessor.Role;
            var actorId = _currentUserAccessor.UserId;
            var ipAddress = _currentUserAccessor.IpAddress;

            var auditResult = BehaviorLog.CreateAudit(
                actionType.ToString(),
                actorRole,
                actorId,
                targetTable,
                targetId,
                oldValuesJson,
                newValuesJson,
                ipAddress);

            if (auditResult.IsSuccess)
            {
                await _context.BehaviorLogs.AddAsync(auditResult.Value, ct);
            }
            else
            {
                throw new ArgumentException($"Failed to create behavior log audit: {auditResult.Error.Message}");
            }
        }
    }
}
