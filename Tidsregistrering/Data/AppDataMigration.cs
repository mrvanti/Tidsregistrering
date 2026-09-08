using Tidsregistrering.Models;

namespace Tidsregistrering.Data;

internal static class AppDataMigration
{
    internal const int CurrentSchemaVersion = 1;

    internal static AppData Migrate(AppData data)
    {
        if (data.SchemaVersion >= CurrentSchemaVersion)
        {
            return data;
        }

        var sessionsByKey = data.AttendanceSessions
            .ToDictionary(session => (session.ExerciseId, session.Date));
        var sessions = data.AttendanceSessions.ToList();
        var entries = data.AttendanceEntries.Select(entry =>
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
            SchemaVersion = CurrentSchemaVersion,
            Exercises = data.Exercises,
            Participants = data.Participants,
            AttendanceSessions = sessions,
            AttendanceEntries = entries
        };
    }
}
