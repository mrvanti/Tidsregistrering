using Tidsregistrering.Models;

namespace Tidsregistrering.Data;

/// <summary>Produces raw attendance CSV only; it never changes stored data.</summary>
public sealed class AttendanceCsvExporter
{
    public string BuildGlobalCsv(IEnumerable<Exercise> exercises, IEnumerable<Participant> participants, IEnumerable<AttendanceEntry> entries)
    {
        var exerciseById = exercises.ToDictionary(exercise => exercise.Id);
        var participantById = participants.ToDictionary(participant => participant.Id);
        var rows = new List<string>
        {
            "Datum,Träning,Veckodag,Tid,Förnamn,Efternamn,YYMMDD,Tränare,Närvarande"
        };

        foreach (var entry in entries.OrderBy(entry => entry.Date).ThenBy(entry => entry.ExerciseId).ThenBy(entry => entry.ParticipantId))
        {
            if (!exerciseById.TryGetValue(entry.ExerciseId, out var exercise) || !participantById.TryGetValue(entry.ParticipantId, out var participant)) continue;
            rows.Add(BuildRow(exercise, participant, entry));
        }

        return string.Join("\r\n", rows) + "\r\n";
    }

    public string BuildExerciseCsv(Exercise exercise, IEnumerable<Participant> participants, IEnumerable<AttendanceEntry> entries)
    {
        var participantById = participants.ToDictionary(participant => participant.Id);
        var rows = new List<string>
        {
            "Datum,Träning,Veckodag,Tid,Förnamn,Efternamn,YYMMDD,Tränare,Närvarande"
        };

        foreach (var entry in entries.OrderBy(entry => entry.Date).ThenBy(entry => entry.ParticipantId))
        {
            if (!participantById.TryGetValue(entry.ParticipantId, out var participant)) continue;

            rows.Add(BuildRow(exercise, participant, entry));
        }

        return string.Join("\r\n", rows) + "\r\n";
    }

    private static string BuildRow(Exercise exercise, Participant participant, AttendanceEntry entry) => string.Join(',',
        Escape(entry.Date.ToString("yyyy-MM-dd")),
        Escape(exercise.Name),
        Escape(exercise.Weekday.ToString()),
        Escape(exercise.Time),
        Escape(participant.FirstName),
        Escape(participant.Surname),
        Escape(participant.DisplayBirthDate),
        participant.IsTrainer ? "Ja" : "Nej",
        entry.IsPresent ? "Ja" : "Nej");

    private static string Escape(string value) => value.IndexOfAny([',', '"', '\r', '\n']) < 0
        ? value
        : $"\"{value.Replace("\"", "\"\"")}\"";
}
