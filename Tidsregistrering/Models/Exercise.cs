namespace Tidsregistrering.Models;

public sealed class Exercise
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string Name { get; init; } = string.Empty;
    public DayOfWeek Weekday { get; init; }
    public string Time { get; init; } = string.Empty;

    public int SwedishWeekdayOrder => Weekday == DayOfWeek.Sunday ? 7 : (int)Weekday;

    public override string ToString() => $"{Weekday} {Time} – {Name}";
}
