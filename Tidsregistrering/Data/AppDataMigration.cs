using Tidsregistrering.Models;

namespace Tidsregistrering.Data;

internal static class AppDataMigration
{
    internal const int CurrentSchemaVersion = 2;

    internal static AppData Migrate(AppData data)
    {
        var exercises = data.Exercises ?? [];
        var legacyParticipants = data.Participants ?? [];
        var participantIdMap = new Dictionary<Guid, Guid>();
        var participants = new List<Participant>();
        foreach (var group in legacyParticipants.GroupBy(participant =>
                     ParticipantRepository.IdentityKey(participant.PersonalNumber) is { } key
                         ? $"pnr:{key}"
                         : $"id:{participant.Id}"))
        {
            var sources = group.ToList();
            var details = sources.Where(participant => !participant.IsArchived)
                .OrderBy(participant => participant.CreatedAtUtc)
                .FirstOrDefault() ?? sources.OrderBy(participant => participant.CreatedAtUtc).First();
            var canonicalId = details.Id;
            foreach (var source in sources) participantIdMap[source.Id] = canonicalId;

            var activeMemberships = sources.Where(participant => !participant.IsArchived)
                .SelectMany(participant => (participant.ExerciseIds ?? []).Concat(
                    participant.ExerciseId == Guid.Empty ? [] : [participant.ExerciseId]))
                .Where(exerciseId => exerciseId != Guid.Empty)
                .Distinct()
                .ToList();
            var archived = activeMemberships.Count == 0 && sources.All(participant => participant.IsArchived);
            participants.Add(new Participant
            {
                Id = canonicalId,
                ExerciseIds = activeMemberships,
                CreatedAtUtc = sources.Min(participant => participant.CreatedAtUtc),
                ArchivedAtUtc = archived ? sources.Max(participant => participant.ArchivedAtUtc) : null,
                FirstName = details.FirstName,
                Surname = details.Surname,
                PersonalNumber = details.PersonalNumber.Trim(),
                IsTrainer = details.IsTrainer,
                IsArchived = archived
            });
        }
        var existingSessions = (data.AttendanceSessions ?? []).OfType<AttendanceSession>();
        var existingEntries = (data.AttendanceEntries ?? []).OfType<AttendanceEntry>();
        var sessionsByKey = new Dictionary<(Guid ExerciseId, DateOnly Date), AttendanceSession>();
        var sessions = new List<AttendanceSession>();
        foreach (var session in existingSessions)
        {
            if (sessionsByKey.TryAdd((session.ExerciseId, session.Date), session))
            {
                sessions.Add(session);
            }
        }

        var entries = existingEntries.Select(entry =>
        {
            if (!sessionsByKey.TryGetValue((entry.ExerciseId, entry.Date), out var session))
            {
                session = new AttendanceSession { ExerciseId = entry.ExerciseId, Date = entry.Date };
                sessionsByKey.Add((entry.ExerciseId, entry.Date), session);
                sessions.Add(session);
            }

            var participantId = participantIdMap.GetValueOrDefault(entry.ParticipantId, entry.ParticipantId);
            return entry.SessionId == Guid.Empty || participantId != entry.ParticipantId
                ? new AttendanceEntry
                {
                    Id = entry.Id,
                    SessionId = session.Id,
                    ExerciseId = entry.ExerciseId,
                    ParticipantId = participantId,
                    Date = entry.Date,
                    IsPresent = entry.IsPresent,
                    RecordedAtUtc = entry.RecordedAtUtc
                }
                : entry;
        })
        .GroupBy(entry => (entry.ExerciseId, entry.ParticipantId, entry.Date))
        .Select(group => group.OrderByDescending(entry => entry.RecordedAtUtc).First())
        .ToList();

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
