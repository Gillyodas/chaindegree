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
        public DateTime IssuedAt { get; private set; }

        // Constructor phục vụ tạo mới thực thể
        private Degree(
            Guid id,
            string degreeCode,
            Guid institutionId,
            Guid signedByRegistrarId,
            Guid studentId,
            string major,
            string classification,
            CryptoSnapshot cryptoData)
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
            IssuedAt = DateTime.UtcNow;
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

            var newDegree = new Degree(
                degreeId,
                degreeCode,
                institutionId,
                signedByRegistrarId,
                studentId,
                major,
                classification,
                cryptoData);

            // Raise domain event
            newDegree.RaiseDomainEvent(new DegreeCreatedEvent(newDegree.Id, newDegree.InstitutionId, newDegree.StudentId, newDegree.DegreeCode));

            return Result<Degree>.Success(newDegree);
        }

        public Result UpdateAcademicDetails(
            string newMajor,
            string newClassification,
            CryptoSnapshot newCryptoData)
        {
            // Nếu bằng đã bị hủy hoàn toàn thì không được phép sửa đổi thông tin nữa
            if (Status == StatusEnum.Revoked)
                return Result.Failure(DegreeErrors.InvalidStateTransition);

            if (string.IsNullOrWhiteSpace(newMajor) || string.IsNullOrWhiteSpace(newClassification))
                return Result.Failure(DegreeErrors.MissingAcademicDetails);

            if (newCryptoData == null || string.IsNullOrEmpty(newCryptoData.DataHashLocal))
                return Result.Failure(DegreeErrors.InvalidCryptoSnapshot);

            // Nếu bằng chưa lên chuỗi (Pending_Confirmation/Confirmation_Error) -> Giữ nguyên để Worker gom lô băm mới.
            // Nếu bằng ĐÃ lên chuỗi ổn định (Confirmed) -> Chuyển sang Pending_Update để kích hoạt Worker cập nhật lại trên Blockchain Besu.
            if (Status == StatusEnum.Confirmed)
            {
                Status = StatusEnum.Pending_Update;
            }

            Major = newMajor;
            Classification = newClassification;
            CryptoData = newCryptoData;
            UpdatedAt = DateTime.UtcNow;

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
        public Result InitiateRevocation()
        {
            if (Status != StatusEnum.Confirmed)
                return Result.Failure(DegreeErrors.InvalidStateTransition);

            Status = StatusEnum.Pending_Revocation;
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        /// <summary>
        /// Thực hiện thu hồi hoàn toàn hiệu lực văn bằng (Xử lý nhanh tại DB hoặc sau khi Worker bắn lệnh thu hồi lên Blockchain thành công)
        /// </summary>
        public Result Revoke()
        {
            // Trường hợp 1: Thu hồi nhanh (Bằng chưa lên chuỗi - Miễn phạt điểm uy tín tại US-2)
            // Trường hợp 2: Hoàn tất thu hồi (Bằng đã lên chuỗi và tiến trình xử lý ngầm hoàn tất)
            if (Status != StatusEnum.Pending_Confirmation && Status != StatusEnum.Pending_Revocation)
                return Result.Failure(DegreeErrors.InvalidStateTransition);

            Status = StatusEnum.Revoked;
            UpdatedAt = DateTime.UtcNow;

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
    }
}