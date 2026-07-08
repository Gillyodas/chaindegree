using System;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.SharedKernel;

namespace ChainDegree.Core.Domain.Degrees.Entities
{
    public class DegreeUpdateRequest : Entity
    {
        public Guid DegreeId { get; private set; }
        public string Major { get; private set; } = null!;
        public string Classification { get; private set; } = null!;
        public CryptoSnapshot CryptoData { get; private set; } = null!;
        public DegreeActionReason Reason { get; private set; } = null!;

        private DegreeUpdateRequest(
            Guid id,
            Guid degreeId,
            string major,
            string classification,
            CryptoSnapshot cryptoData,
            DegreeActionReason reason)
        {
            Id = id;
            DegreeId = degreeId;
            Major = major;
            Classification = classification;
            CryptoData = cryptoData;
            Reason = reason;
            CreatedAt = DateTime.UtcNow;
        }

        private DegreeUpdateRequest() { }

        public static DegreeUpdateRequest Create(
            Guid degreeId,
            string major,
            string classification,
            CryptoSnapshot cryptoData,
            DegreeActionReason reason)
        {
            return new DegreeUpdateRequest(
                Guid.NewGuid(),
                degreeId,
                major,
                classification,
                cryptoData,
                reason);
        }
    }
}
