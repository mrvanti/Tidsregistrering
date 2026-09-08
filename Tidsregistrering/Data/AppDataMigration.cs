using Tidsregistrering.Models;

namespace Tidsregistrering.Data;

internal static class AppDataMigration
{
    internal const int CurrentSchemaVersion = 1;

    internal static AppData Migrate(AppData data)
    {
        var exercises = data.Exercises ?? [];
        var participants = data.Participants ?? [];
        var existingSessions = data.AttendanceSessions ?? [];
        var existingEntries = data.AttendanceEntries ?? [];

        var sessionsByKey = existingSessions
            .ToDictionary(session => (session.ExerciseId, session.Date));
        var sessions = existingSessions.ToList();
        var entries = existingEntries.Select(entry =>
        {
            if (!sessionsByKey.TryGetValue((entry.ExerciseId, entry.Date), out var session))
            {
                session = new AttendanceSession { ExerciseId = entry.ExerciseId, Date = entry.Date };
                sessionsByKey.Add((entry.ExerciseId, entry.Date), session);
                sessions.Add(session);
            }

            return entry.SessionId == Guid.Empty
                ? new AttendanceEntry
                {
                    Id = entry.Id,
                    SessionId = session.Id,
                    ExerciseId = entry.ExerciseId,
                    ParticipantId = entry.ParticipantId,
                    Date = entry.Date,
                    IsPresent = entry.IsPresent,
                    RecordedAtUtc = entry.RecordedAtUtc
                }
                : entry;
        }).ToList();

        return new AppData
        {
            SchemaVersion = Math.Max(data.SchemaVersion, CurrentSchemaVersion),
            Exercises = exercises,
            Participants = participants,
            AttendanceSessions = sessions,
            AttendanceEntries = entries
        };
    }
}
