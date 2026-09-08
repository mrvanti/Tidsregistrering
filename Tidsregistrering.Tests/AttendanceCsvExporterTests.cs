using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Tests;

[TestClass]
public sealed class AttendanceCsvExporterTests
{
    [TestMethod]
    public void BuildExerciseCsv_WritesRawRowsAndEscapesCsvValues()
    {
        var exercise = new Exercise { Name = "Barn, nybörjare", Time = "18:00", Weekday = DayOfWeek.Monday };
        var participant = new Participant { FirstName = "Ada", Surname = "\"Lind\"", PersonalNumber = "200101011234", IsTrainer = true };
        var entry = new AttendanceEntry { ExerciseId = exercise.Id, ParticipantId = participant.Id, Date = new DateOnly(2026, 9, 8), IsPresent = true };

        var csv = new AttendanceCsvExporter().BuildExerciseCsv(exercise, [participant], [entry]);

        StringAssert.Contains(csv, "Datum,Träning,Veckodag,Tid,Förnamn,Efternamn,YYMMDD,Tränare,Närvarande\r\n");
        StringAssert.Contains(csv, "2026-09-08,\"Barn, nybörjare\",Monday,18:00,Ada,\"\"\"Lind\"\"\",010101,Ja,Ja\r\n");
    }

    [TestMethod]
    public void BuildGlobalCsv_IncludesRowsFromMultipleExercises()
    {
        var first = new Exercise { Name = "First" };
        var second = new Exercise { Name = "Second" };
        var firstParticipant = new Participant { ExerciseId = first.Id, FirstName = "Ada" };
        var secondParticipant = new Participant { ExerciseId = second.Id, FirstName = "Bo" };
        var entries = new[]
        {
            new AttendanceEntry { ExerciseId = first.Id, ParticipantId = firstParticipant.Id, Date = new DateOnly(2026, 9, 8), IsPresent = true },
            new AttendanceEntry { ExerciseId = second.Id, ParticipantId = secondParticipant.Id, Date = new DateOnly(2026, 9, 9), IsPresent = true }
        };

        var csv = new AttendanceCsvExporter().BuildGlobalCsv([first, second], [firstParticipant, secondParticipant], entries);

        StringAssert.Contains(csv, "2026-09-08,First");
        StringAssert.Contains(csv, "2026-09-09,Second");
    }
}
