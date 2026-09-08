namespace Tidsregistrering.Models;

public sealed class AttendanceEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    // Legacy entries may have no session ID. Storage migration will populate it.
    public Guid SessionId { get; init; }
    public Guid ExerciseId { get; init; }
    public Guid ParticipantId { get; init; }
    public DateOnly Date { get; init; }
    public bool IsPresent { get; init; }
    public DateTimeOffset RecordedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
