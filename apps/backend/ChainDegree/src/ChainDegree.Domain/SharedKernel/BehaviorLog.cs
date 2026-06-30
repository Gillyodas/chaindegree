using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.SharedKernel.Enums;

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

        // Hàm hỗ trợ khởi tạo nhanh một bản ghi nhật ký hệ thống
        public static BehaviorLog CreateAudit(
            string actionType,
            string actorRole,
            Guid actorId,
            string targetTable,
            Guid targetId,
            string? oldValues,
            string newValues,
            string ipAddress)
        {
            throw new NotImplementedException();
        }
    }
}
