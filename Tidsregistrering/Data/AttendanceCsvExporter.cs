using Tidsregistrering.Models;

namespace Tidsregistrering.Data;

/// <summary>Produces raw attendance and transparent LOK allocation CSV without changing stored data.</summary>
public sealed class AttendanceCsvExporter
{
    private readonly LokGroupAllocationEngine allocationEngine = new();

    public string BuildGlobalCsv(IEnumerable<Exercise> exercises, IEnumerable<Participant> participants, IEnumerable<AttendanceEntry> entries)
    {
        var exerciseById = exercises.ToDictionary(exercise => exercise.Id);
        var participantById = participants.ToDictionary(participant => participant.Id);
        var rows = new List<string> { "Datum,Träning,Veckodag,Tid,Förnamn,Efternamn,YYMMDD,Tränare,Närvarande" };
        foreach (var entry in entries.OrderBy(entry => entry.Date).ThenBy(entry => entry.ExerciseId).ThenBy(entry => entry.ParticipantId))
        {
            if (exerciseById.TryGetValue(entry.ExerciseId, out var exercise) && participantById.TryGetValue(entry.ParticipantId, out var participant))
            {
                rows.Add(BuildRow(exercise, participant, entry));
            }
        }

        return string.Join("\r\n", rows) + "\r\n";
    }

    public string BuildExerciseCsv(Exercise exercise, IEnumerable<Participant> participants, IEnumerable<AttendanceEntry> entries)
    {
        var participantById = participants.ToDictionary(participant => participant.Id);
        var entryList = entries.ToList();
        var rows = new List<string> { "Datum,Träning,Veckodag,Tid,Förnamn,Efternamn,YYMMDD,Tränare,Närvarande" };
        foreach (var entry in entryList.OrderBy(entry => entry.Date).ThenBy(entry => entry.ParticipantId))
        {
            if (participantById.TryGetValue(entry.ParticipantId, out var participant)) rows.Add(BuildRow(exercise, participant, entry));
        }

        rows.Add(string.Empty);
        rows.Add("LOK-gruppering (endast verkligt separata aktiviteter)");
        rows.Add("Ålderskontroll ej tillämpad; alla närvarande räknas som behöriga.");
        rows.Add("Datum,Grupp,Roll,Förnamn,Efternamn,YYMMDD");
        foreach (var dateEntries in entryList.Where(entry => entry.IsPresent).GroupBy(entry => entry.Date).OrderBy(group => group.Key))
        {
            var assessments = dateEntries.Where(entry => participantById.ContainsKey(entry.ParticipantId)).Select(entry => new LokEligibilityResult
            {
                ParticipantId = entry.ParticipantId,
                IsEligible = true,
                IsLeader = participantById[entry.ParticipantId].IsTrainer,
                Reason = "Ålderskontroll ej tillämpad"
            });
            AppendAllocationRows(rows, allocationEngine.Allocate(exercise.Id, dateEntries.Key, assessments), participantById);
        }

        return string.Join("\r\n", rows) + "\r\n";
    }

    private static string BuildRow(Exercise exercise, Participant participant, AttendanceEntry entry) => string.Join(',', Escape(entry.Date.ToString("yyyy-MM-dd")), Escape(exercise.Name), Escape(exercise.Weekday.ToString()), Escape(exercise.Time), Escape(participant.FirstName), Escape(participant.Surname), Escape(participant.DisplayBirthDate), participant.IsTrainer ? "Ja" : "Nej", entry.IsPresent ? "Ja" : "Nej");

    private static void AppendAllocationRows(List<string> rows, ExportGroupingResult allocation, IReadOnlyDictionary<Guid, Participant> participantById)
    {
        foreach (var group in allocation.Groups)
        {
            foreach (var leaderId in group.LeaderIds) AppendAllocationRow(rows, allocation.Date, group.Number.ToString(), "Ledare", leaderId, participantById);
            foreach (var participantId in group.ParticipantIds) AppendAllocationRow(rows, allocation.Date, group.Number.ToString(), "Deltagare", participantId, participantById);
        }

        foreach (var leaderId in allocation.UnassignedLeaderIds) AppendAllocationRow(rows, allocation.Date, string.Empty, "Ej tilldelad ledare", leaderId, participantById);
        foreach (var participantId in allocation.UnassignedParticipantIds) AppendAllocationRow(rows, allocation.Date, string.Empty, "Ej tilldelad deltagare", participantId, participantById);
    }

    private static void AppendAllocationRow(List<string> rows, DateOnly date, string group, string role, Guid participantId, IReadOnlyDictionary<Guid, Participant> participantById)
    {
        if (participantById.TryGetValue(participantId, out var participant)) rows.Add(string.Join(',', Escape(date.ToString("yyyy-MM-dd")), Escape(group), Escape(role), Escape(participant.FirstName), Escape(participant.Surname), Escape(participant.DisplayBirthDate)));
    }

    private static string Escape(string value) => value.IndexOfAny([',', '"', '\r', '\n']) < 0 ? value : $"\"{value.Replace("\"", "\"\"")}\"";
}
