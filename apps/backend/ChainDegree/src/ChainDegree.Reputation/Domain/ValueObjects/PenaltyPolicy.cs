using System;
using ChainDegree.Reputation.Domain.Enums;

namespace ChainDegree.Reputation.Domain.ValueObjects;

public record PenaltyRule(int ScoreDeduction, bool TriggersFreeze);

public static class PenaltyPolicy
{
    public static PenaltyRule GetRule(PenaltyReasonEnum reasonCode)
    {
        return reasonCode switch
        {
            PenaltyReasonEnum.S01_IdentityInformationError => new PenaltyRule(20, false),
            PenaltyReasonEnum.S02_AcademicResultError => new PenaltyRule(20, false),
            PenaltyReasonEnum.R01_FraudulentData => new PenaltyRule(400, true),
            PenaltyReasonEnum.R02_OutdatedCurriculum => new PenaltyRule(150, false),
            PenaltyReasonEnum.H01_SystemCompromised => new PenaltyRule(0, true),
            PenaltyReasonEnum.Shortcut_Exemption => new PenaltyRule(0, false),
            _ => throw new ArgumentOutOfRangeException(nameof(reasonCode), $"Unsupported penalty reason: {reasonCode}")
        };
    }
}
