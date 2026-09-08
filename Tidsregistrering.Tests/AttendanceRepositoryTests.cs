using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Tests;

[TestClass]
public sealed class AttendanceRepositoryTests
{
    [TestMethod]
    public void SetAttendance_ReusesSessionAndReplacesParticipantState()
    {
        var data = new AppData();
        var repository = new AttendanceRepository(data);
        var exerciseId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 8);

        repository.SetAttendance(exerciseId, participantId, date, true);
        repository.SetAttendance(exerciseId, participantId, date, false);

        Assert.AreEqual(1, data.AttendanceSessions.Count);
        Assert.AreEqual(1, data.AttendanceEntries.Count);
        Assert.IsFalse(data.AttendanceEntries[0].IsPresent);
        Assert.AreEqual(data.AttendanceSessions[0].Id, data.AttendanceEntries[0].SessionId);
    }

    [TestMethod]
    public void GetRawAttendance_RestrictsExerciseAndInclusiveDateRange()
    {
        var data = new AppData();
        var repository = new AttendanceRepository(data);
        var exerciseId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        repository.SetAttendance(exerciseId, participantId, new DateOnly(2026, 9, 1), true);
        repository.SetAttendance(exerciseId, participantId, new DateOnly(2026, 9, 2), true);
        repository.SetAttendance(Guid.NewGuid(), participantId, new DateOnly(2026, 9, 2), true);

        var entries = repository.GetRawAttendance(exerciseId, new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 2));

        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(new DateOnly(2026, 9, 2), entries[0].Date);
    }

    [TestMethod]
    public void ClearAll_RemovesOnlyAttendance()
    {
        var participant = new Participant { FirstName = "Ada" };
        var exercise = new Exercise { Name = "Judo" };
        var data = new AppData { Exercises = [exercise], Participants = [participant] };
        var repository = new AttendanceRepository(data);
        repository.SetAttendance(exercise.Id, participant.Id, new DateOnly(2026, 9, 8), true);

        Assert.AreEqual(1, repository.ClearAll());
        Assert.AreEqual(0, data.AttendanceEntries.Count);
        Assert.AreEqual(0, data.AttendanceSessions.Count);
        Assert.AreEqual(1, data.Exercises.Count);
        Assert.AreEqual(1, data.Participants.Count);
    }
}
