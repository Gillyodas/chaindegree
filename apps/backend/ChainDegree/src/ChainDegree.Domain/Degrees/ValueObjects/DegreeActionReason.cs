using System;
using System.Collections.Generic;

namespace ChainDegree.Core.Domain.Degrees.ValueObjects
{
    public class DegreeActionReason
    {
        public string Code { get; private set; } = null!;
        public string Description { get; private set; } = null!;

        public static readonly DegreeActionReason AdministrativeError1 = new("S-01", "Administrative Error - Incorrect name/classification");
        public static readonly DegreeActionReason AdministrativeError2 = new("S-02", "Administrative Error - System entry duplicate");
        public static readonly DegreeActionReason FraudulentData1 = new("R-01", "Fraudulent Data - Academic credentials forgery");
        public static readonly DegreeActionReason FraudulentData2 = new("R-02", "Fraudulent Data - Forged identity");
        public static readonly DegreeActionReason SystemCompromise = new("H-01", "System Compromise / Hack");

        private static readonly Dictionary<string, DegreeActionReason> Reasons = new(StringComparer.OrdinalIgnoreCase)
        {
            { AdministrativeError1.Code, AdministrativeError1 },
            { AdministrativeError2.Code, AdministrativeError2 },
            { FraudulentData1.Code, FraudulentData1 },
            { FraudulentData2.Code, FraudulentData2 },
            { SystemCompromise.Code, SystemCompromise }
        };

        private DegreeActionReason(string code, string description)
        {
            Code = code;
            Description = description;
        }

        private DegreeActionReason() { }

        public static DegreeActionReason FromCode(string code, string? customDescription = null)
        {
            if (Reasons.TryGetValue(code, out var reason))
            {
                return reason;
            }
            return new DegreeActionReason(code, customDescription ?? "Unknown reason");
        }

        public override bool Equals(object? obj)
        {
            if (obj is not DegreeActionReason other) return false;
            return string.Equals(Code, other.Code, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Code.ToLowerInvariant().GetHashCode();
        }
    }
}
