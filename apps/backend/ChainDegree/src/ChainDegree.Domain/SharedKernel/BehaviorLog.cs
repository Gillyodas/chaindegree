using System;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.Result;
using ChainDegree.SharedKernel.DomainErrors.BehaviorLog;

namespace ChainDegree.Core.Domain.SharedKernel
{
    public class BehaviorLog
    {
        public Guid Id { get; private set; }
        public ActionTypeEnum ActionType { get; private set; }
        public string ActorRole { get; private set; } = null!;
        public Guid ActorId { get; private set; }
        public string TargetTable { get; private set; } = null!;
        public Guid TargetId { get; private set; }
        public string? OldValuesJson { get; private set; }
        public string NewValuesJson { get; private set; } = null!;
        public string IpAddress { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; }

        private BehaviorLog() { }

        // Hàm hỗ trợ khởi tạo nhanh một bản ghi nhật ký hệ thống
        public static Result<BehaviorLog> CreateAudit(
            string actionType,
            string actorRole,
            Guid actorId,
            string targetTable,
            Guid targetId,
            string? oldValues,
            string newValues,
            string ipAddress)
        {
            if (!Enum.TryParse<ActionTypeEnum>(actionType, out var actionTypeEnum))
            {
                return Result<BehaviorLog>.Failure(BehaviorLogErrors.InvalidActionType);
            }

            if (actorId == Guid.Empty || string.IsNullOrWhiteSpace(actorRole))
            {
                return Result<BehaviorLog>.Failure(BehaviorLogErrors.EmptyActorInfo);
            }

            if (targetId == Guid.Empty || string.IsNullOrWhiteSpace(targetTable))
            {
                return Result<BehaviorLog>.Failure(BehaviorLogErrors.EmptyTargetInfo);
            }

            var log = new BehaviorLog
            {
                Id = Guid.NewGuid(),
                ActionType = actionTypeEnum,
                ActorRole = actorRole,
                ActorId = actorId,
                TargetTable = targetTable,
                TargetId = targetId,
                OldValuesJson = oldValues,
                NewValuesJson = newValues,
                IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            return Result<BehaviorLog>.Success(log);
        }
    }
}
