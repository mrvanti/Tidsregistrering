namespace Tidsregistrering.Models;

/// <summary>Export-time eligibility assessment; raw attendance remains untouched.</summary>
public sealed class LokEligibilityResult
{
    public Guid ParticipantId { get; init; }
    public bool IsEligible { get; init; }
    public bool IsLeader { get; init; }
    public string Reason { get; init; } = string.Empty;
}
