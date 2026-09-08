using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Tests;

[TestClass]
public sealed class AppDataPersistenceTests
{
    [TestMethod]
    public void JsonRoundTrip_PreservesRosterSessionsAndAttendance()
    {
        var exercise = new Exercise { Name = "Judo", Time = "18:00", Weekday = DayOfWeek.Monday };
        var participant = new Participant { ExerciseId = exercise.Id, FirstName = "Ada", Surname = "Lind", PersonalNumber = "200101011234" };
        var data = new AppData { Exercises = [exercise], Participants = [participant] };
        var attendance = new AttendanceRepository(data);
        attendance.SetAttendance(exercise.Id, participant.Id, new DateOnly(2026, 9, 8), true);

        var restored = JsonSerializer.Deserialize<AppData>(JsonSerializer.Serialize(data))!;
        restored = AppDataMigration.Migrate(restored);

        Assert.AreEqual(exercise.Id, restored.Exercises[0].Id);
        Assert.AreEqual(participant.Id, restored.Participants[0].Id);
        Assert.AreEqual(1, restored.AttendanceSessions.Count);
        Assert.AreEqual(restored.AttendanceSessions[0].Id, restored.AttendanceEntries[0].SessionId);
        Assert.IsTrue(restored.AttendanceEntries[0].IsPresent);
    }
}
