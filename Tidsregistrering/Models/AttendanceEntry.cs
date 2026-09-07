namespace Tidsregistrering.Models;

public sealed class AttendanceEntry
{
    public Guid ExerciseId { get; init; }
    public Guid ParticipantId { get; init; }
    public DateOnly Date { get; init; }
    public bool IsPresent { get; init; }
}
