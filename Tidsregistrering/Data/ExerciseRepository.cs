using Tidsregistrering.Models;

namespace Tidsregistrering.Data;

/// <summary>Exercise-owned operations over the local aggregate.</summary>
public sealed class ExerciseRepository(AppData data)
{
    public IReadOnlyList<Exercise> ListSorted() => data.Exercises
        .OrderBy(exercise => exercise.SwedishWeekdayOrder)
        .ThenBy(exercise => TimeOnly.TryParse(exercise.Time, out var time) ? time : TimeOnly.MaxValue)
        .ThenBy(exercise => exercise.Name, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    public Exercise? Get(Guid id) => data.Exercises.FirstOrDefault(exercise => exercise.Id == id);

    /// <summary>Rejects an identical weekday, time, and name (case-insensitive).</summary>
    public bool TryAdd(Exercise exercise)
    {
        if (data.Exercises.Any(existing =>
                existing.Weekday == exercise.Weekday &&
                string.Equals(existing.Time, exercise.Time, StringComparison.Ordinal) &&
                string.Equals(existing.Name, exercise.Name, StringComparison.CurrentCultureIgnoreCase)))
        {
            return false;
        }

        data.Exercises.Add(exercise);
        return true;
    }

    public int CountAttendance(Guid id) => data.AttendanceEntries.Count(entry => entry.ExerciseId == id);

    /// <summary>Removes an exercise and all records owned exclusively by it.</summary>
    public bool Delete(Guid id)
    {
        if (data.Exercises.RemoveAll(exercise => exercise.Id == id) == 0)
        {
            return false;
        }

        data.Participants.RemoveAll(participant => participant.ExerciseId == id);
        data.AttendanceEntries.RemoveAll(entry => entry.ExerciseId == id);
        data.AttendanceSessions.RemoveAll(session => session.ExerciseId == id);
        return true;
    }
}
