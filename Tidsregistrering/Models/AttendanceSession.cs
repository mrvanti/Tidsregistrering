namespace Tidsregistrering.Models;

/// <summary>One dated occurrence of an exercise. Attendance entries belong to this session.</summary>
public sealed class AttendanceSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ExerciseId { get; init; }
    public DateOnly Date { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
