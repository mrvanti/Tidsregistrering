namespace Tidsregistrering.Models;

/// <summary>
/// Export-time allocation only. It never changes stored attendance.
/// Unassigned IDs make incomplete groups explicit.
/// </summary>
public sealed class ExportGroupingResult
{
    public Guid ExerciseId { get; init; }
    public DateOnly Date { get; init; }
    public List<ExportGroup> Groups { get; init; } = [];
    public List<Guid> UnassignedParticipantIds { get; init; } = [];
    public List<Guid> UnassignedLeaderIds { get; init; } = [];
}

public sealed class ExportGroup
{
    public int Number { get; init; }
    public List<Guid> LeaderIds { get; init; } = [];
    public List<Guid> ParticipantIds { get; init; } = [];
}
