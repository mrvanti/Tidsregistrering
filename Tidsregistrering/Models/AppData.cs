namespace Tidsregistrering.Models;

public sealed class AppData
{
    public List<Exercise> Exercises { get; init; } = [];
    public List<Participant> Participants { get; init; } = [];
    public List<AttendanceEntry> AttendanceEntries { get; init; } = [];
}
