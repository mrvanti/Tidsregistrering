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

    /// <summary>Adds one exercise for every weekday, atomically.</summary>
    public bool TryAddMany(string name, string time, IReadOnlyCollection<DayOfWeek> weekdays, out Exercise? firstAdded)
    {
        firstAdded = null;
        var additions = weekdays.Distinct().Select(weekday => new Exercise { Name = name, Time = time, Weekday = weekday }).ToList();
        if (additions.Count == 0 || additions.Any(addition => data.Exercises.Any(existing => IsDuplicate(existing, addition.Name, addition.Time, addition.Weekday))))
        {
            return false;
        }

        data.Exercises.AddRange(additions);
        firstAdded = additions[0];
        return true;
    }

    /// <summary>Updates selected exercise and adds copies for extra selected weekdays, without deleting records.</summary>
    public bool TryUpdateAndAddWeekdays(Guid id, string name, string time, IReadOnlyCollection<DayOfWeek> weekdays, out Exercise? updated)
    {
        updated = null;
        var existing = Get(id);
        var selected = weekdays.Distinct().ToList();
        if (existing is null || selected.Count == 0)
        {
            return false;
        }

        var primaryWeekday = selected.Contains(existing.Weekday) ? existing.Weekday : selected[0];
        var additions = selected.Where(weekday => weekday != primaryWeekday)
            .Select(weekday => new Exercise { Name = name, Time = time, Weekday = weekday }).ToList();
        if (data.Exercises.Any(candidate => candidate.Id != id && IsDuplicate(candidate, name, time, primaryWeekday)) ||
            additions.Any(addition => data.Exercises.Any(candidate => candidate.Id != id && IsDuplicate(candidate, addition.Name, addition.Time, addition.Weekday))))
        {
            return false;
        }

        updated = new Exercise { Id = existing.Id, CreatedAtUtc = existing.CreatedAtUtc, Name = name, Time = time, Weekday = primaryWeekday };
        data.Exercises[data.Exercises.IndexOf(existing)] = updated;
        data.Exercises.AddRange(additions);
        return true;
    }

    public int CountAttendance(Guid id) => data.AttendanceEntries.Count(entry => entry.ExerciseId == id);

    /// <summary>Updates an exercise while retaining its identity and associated records.</summary>
    public bool TryUpdate(Guid id, string name, string time, DayOfWeek weekday)
    {
        var existing = Get(id);
        if (existing is null || data.Exercises.Any(candidate => candidate.Id != id && IsDuplicate(candidate, name, time, weekday)))
        {
            return false;
        }

        var index = data.Exercises.IndexOf(existing);
        data.Exercises[index] = new Exercise
        {
            Id = existing.Id,
            CreatedAtUtc = existing.CreatedAtUtc,
            Name = name,
            Time = time,
            Weekday = weekday
        };
        return true;
    }

    private static bool IsDuplicate(Exercise exercise, string name, string time, DayOfWeek weekday) =>
        exercise.Weekday == weekday &&
        string.Equals(exercise.Time, time, StringComparison.Ordinal) &&
        string.Equals(exercise.Name, name, StringComparison.CurrentCultureIgnoreCase);

    /// <summary>Removes an exercise and its memberships/attendance while retaining global people.</summary>
    public bool Delete(Guid id)
    {
        if (data.Exercises.RemoveAll(exercise => exercise.Id == id) == 0)
        {
            return false;
        }

        for (var index = 0; index < data.Participants.Count; index++)
        {
            var participant = data.Participants[index];
            var existingMemberships = participant.ExerciseIds
                .Concat(participant.ExerciseId == Guid.Empty ? [] : [participant.ExerciseId])
                .Distinct()
                .ToList();
            if (!existingMemberships.Contains(id)) continue;

            var memberships = existingMemberships
                .Where(exerciseId => exerciseId != id)
                .ToList();
            var archived = memberships.Count == 0;
            data.Participants[index] = new Participant
            {
                Id = participant.Id,
                ExerciseIds = memberships,
                CreatedAtUtc = participant.CreatedAtUtc,
                ArchivedAtUtc = archived ? DateTimeOffset.UtcNow : null,
                FirstName = participant.FirstName,
                Surname = participant.Surname,
                PersonalNumber = participant.PersonalNumber,
                IsTrainer = participant.IsTrainer,
                IsArchived = archived
            };
        }
        data.AttendanceEntries.RemoveAll(entry => entry.ExerciseId == id);
        data.AttendanceSessions.RemoveAll(session => session.ExerciseId == id);
        return true;
    }
}
