using System;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.Degrees.Events;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Domain.Degrees
{
    public class Degree : AggregateRoot, IInstitutionScoped
    {
        public string DegreeCode { get; private set; } = null!;
        public Guid InstitutionId { get; private set; }
        public Guid SignedByRegistrarId { get; private set; }
        public Guid StudentId { get; private set; }
        public string Major { get; private set; } = null!;
        public string Classification { get; private set; } = null!;
        public CryptoSnapshot CryptoData { get; private set; } = null!;
        public StatusEnum Status { get; private set; }
        public string? TxHashBlockchain { get; private set; }
        public int CurrentVersion { get; private set; }
        public DateTime IssuedAt { get; private set; }
        public byte[] RowVersion { get; private set; } = null!;

        // Constructor phục vụ tạo mới thực thể
        private Degree(
            Guid id,
            string degreeCode,
            Guid institutionId,
            Guid signedByRegistrarId,
            Guid studentId,
            string major,
            string classification,
            CryptoSnapshot cryptoData,
            DateTime? issuedAt = null)
        {
            Id = id;
            DegreeCode = degreeCode;
            InstitutionId = institutionId;
            SignedByRegistrarId = signedByRegistrarId;
            StudentId = studentId;
            Major = major;
            Classification = classification;
            CryptoData = cryptoData;
            Status = StatusEnum.Pending_Confirmation; // Mặc định khi tạo mới là chờ gom lô lên chuỗi
            IssuedAt = issuedAt ?? DateTime.UtcNow;
            CurrentVersion = 1;
        }

        private Degree() { }

        private static string GenerateDegreeCode(long totalDegree)
        {
            return $"DEG-{DateTime.UtcNow.Year}-{(totalDegree + 1):D6}";
        }

        /// <summary>
        /// Factory Method khởi tạo văn bằng mới (US-1)
        /// </summary>
        public static Result<Degree> Create(
            long totalDegree,
            Guid institutionId,
            Guid signedByRegistrarId,
            Guid studentId,
            string major,
            string classification,
            CryptoSnapshot cryptoData)
        {
            if (totalDegree < 0)
                return Result<Degree>.Failure(DegreeErrors.InvalidTotalCount);

            if (institutionId == Guid.Empty || signedByRegistrarId == Guid.Empty || studentId == Guid.Empty)
                return Result<Degree>.Failure(DegreeErrors.EmptyIdentifiers);

            if (string.IsNullOrWhiteSpace(major) || string.IsNullOrWhiteSpace(classification))
                return Result<Degree>.Failure(DegreeErrors.MissingAcademicDetails);

            if (cryptoData == null || string.IsNullOrEmpty(cryptoData.DataHashLocal))
                return Result<Degree>.Failure(DegreeErrors.InvalidCryptoSnapshot);

            Guid degreeId = Guid.NewGuid();
            string degreeCode = GenerateDegreeCode(totalDegree);

            // Parse IssuedAt from PlainDataJson to synchronize with calculated hash
            DateTime? issuedAt = null;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(cryptoData.PlainDataJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("issuedAt", out var prop) && DateTime.TryParse(prop.GetString(), out var parsedDate))
                {
                    issuedAt = parsedDate;
                }
                else if (root.TryGetProperty("IssuedAt", out var prop2) && DateTime.TryParse(prop2.GetString(), out var parsedDate2))
                {
                    issuedAt = parsedDate2;
                }
            }
            catch { }

            var newDegree = new Degree(
                degreeId,
                degreeCode,
                institutionId,
                signedByRegistrarId,
                studentId,
                major,
                classification,
                cryptoData,
                issuedAt);

            // Raise domain event
            newDegree.RaiseDomainEvent(new DegreeCreatedEvent(newDegree.Id, newDegree.InstitutionId, newDegree.StudentId, newDegree.DegreeCode));

            return Result<Degree>.Success(newDegree);
        }

        /// <summary>
        /// Kích hoạt tiến trình yêu cầu cập nhật văn bằng đích danh bất đồng bộ (US-2 - Kịch bản bằng đã lên chuỗi)
        /// </summary>
        public Result InitiateUpdate(string newHash, DegreeActionReason reason)
        {
            if (Status != StatusEnum.Confirmed)
                return Result.Failure(DegreeErrors.InvalidStateTransition);

            if (string.IsNullOrWhiteSpace(newHash))
                return Result.Failure(DegreeErrors.InvalidCryptoSnapshot);

            Status = StatusEnum.Pending_Update;
            CurrentVersion++;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new DegreeUpdatedEvent(Id, InstitutionId, reason.Code, CryptoData.DataHashLocal, newHash));

            return Result.Success();
        }

        /// <summary>
        /// Xác nhận cập nhật thông tin và neo chặn Merkle Root mới thành công lên Blockchain (Worker gọi)
        /// </summary>
        public Result ConfirmUpdate(string newMajor, string newClassification, CryptoSnapshot newCryptoData, string txHash)
        {
            if (string.IsNullOrWhiteSpace(txHash))
                return Result.Failure(DegreeErrors.EmptyTransactionHash);

            if (Status != StatusEnum.Pending_Update)
                return Result.Failure(DegreeErrors.InvalidStateTransition);

            if (string.IsNullOrWhiteSpace(newMajor) || string.IsNullOrWhiteSpace(newClassification))
                return Result.Failure(DegreeErrors.MissingAcademicDetails);

            if (newCryptoData == null || string.IsNullOrEmpty(newCryptoData.DataHashLocal))
                return Result.Failure(DegreeErrors.InvalidCryptoSnapshot);

            Major = newMajor;
            Classification = newClassification;
            CryptoData = newCryptoData;
            TxHashBlockchain = txHash;
            Status = StatusEnum.Confirmed;
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        /// <summary>
        /// Cập nhật trực tiếp thông tin văn bằng chưa lên chuỗi (Shortcut logic - US-2)
        /// </summary>
        public Result UpdateShortcut(string newMajor, string newClassification, CryptoSnapshot newCryptoData, DegreeActionReason reason)
        {
            if (Status != StatusEnum.Pending_Confirmation && Status != StatusEnum.Confirmation_Error)
                return Result.Failure(DegreeErrors.InvalidStateTransition);

            if (string.IsNullOrWhiteSpace(newMajor) || string.IsNullOrWhiteSpace(newClassification))
                return Result.Failure(DegreeErrors.MissingAcademicDetails);

            if (newCryptoData == null || string.IsNullOrEmpty(newCryptoData.DataHashLocal))
                return Result.Failure(DegreeErrors.InvalidCryptoSnapshot);

            var previousHash = CryptoData.DataHashLocal;

            Major = newMajor;
            Classification = newClassification;
            CryptoData = newCryptoData;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new DegreeUpdatedWithoutConfirmationEvent(Id, InstitutionId, reason.Code, previousHash, newCryptoData.DataHashLocal));

            return Result.Success();
        }

        /// <summary>
        /// Xác nhận đã đồng bộ và neo chặn Merkle Root thành công lên Blockchain Hyperledger Besu (Xử lý ngầm từ Worker)
        /// </summary>
        public Result ConfirmBlockchainSync(string txHash)
        {
            if (string.IsNullOrWhiteSpace(txHash))
                return Result.Failure(DegreeErrors.EmptyTransactionHash);

            // Chỉ cho phép chuyển sang Confirmed từ trạng thái Pending_Confirmation
            if (Status != StatusEnum.Pending_Confirmation)
                return Result.Failure(DegreeErrors.InvalidStateTransition);

            TxHashBlockchain = txHash;
            Status = StatusEnum.Confirmed;
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        /// <summary>
        /// Kích hoạt tiến trình yêu cầu thu hồi văn bằng đích danh bất đồng bộ (US-2 - Kịch bản bằng đã lên chuỗi)
        /// </summary>
        public Result InitiateRevocation(DegreeActionReason reason)
        {
            if (Status != StatusEnum.Confirmed)
                return Result.Failure(DegreeErrors.InvalidStateTransition);

            Status = StatusEnum.Pending_Revocation;
            CurrentVersion++;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new DegreeRevokedEvent(Id, InstitutionId, reason.Code));

            return Result.Success();
        }

        /// <summary>
        /// Xác nhận đã đồng bộ giao dịch thu hồi lên Blockchain thành công (Worker gọi)
        /// </summary>
        public Result ConfirmRevocation(string txHash)
        {
            if (string.IsNullOrWhiteSpace(txHash))
                return Result.Failure(DegreeErrors.EmptyTransactionHash);

            if (Status != StatusEnum.Pending_Revocation)
                return Result.Failure(DegreeErrors.InvalidStateTransition);

            TxHashBlockchain = txHash;
            Status = StatusEnum.Revoked;
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        /// <summary>
        /// Thu hồi nhanh văn bằng chưa được neo chặn lên chuỗi (Shortcut logic - US-2)
        /// </summary>
        public Result RevokeShortcut(DegreeActionReason reason)
        {
            if (Status != StatusEnum.Pending_Confirmation && Status != StatusEnum.Confirmation_Error)
                return Result.Failure(DegreeErrors.InvalidStateTransition);

            Status = StatusEnum.Revoked;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new DegreeRevokedWithoutConfirmationEvent(Id, InstitutionId, reason.Code));

            return Result.Success();
        }

        /// <summary>
        /// Đánh dấu tiến trình đồng bộ chuỗi bị lỗi để hệ thống ngầm tiến hành quét lại (Retry)
        /// </summary>
        public void MarkAsSyncError()
        {
            if (Status == StatusEnum.Pending_Confirmation)
            {
                Status = StatusEnum.Confirmation_Error;
                UpdatedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Chuyển degree từ Confirmation_Error → Pending_Confirmation để requeue cho worker gom lô lại
        /// </summary>
        public Result MarkReadyForRetry()
        {
            if (Status != StatusEnum.Confirmation_Error)
                return Result.Failure(DegreeErrors.InvalidStateTransition);

            Status = StatusEnum.Pending_Confirmation;
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }
    }
}