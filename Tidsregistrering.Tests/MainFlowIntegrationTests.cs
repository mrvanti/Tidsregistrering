using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Tests;

[TestClass]
public sealed class MainFlowIntegrationTests
{
    [TestMethod]
    public void SelectedExercise_OwnsNewParticipantAndAttendanceToggle()
    {
        var selectedExercise = new Exercise { Name = "Senior", Time = "18:00", Weekday = DayOfWeek.Monday };
        var otherExercise = new Exercise { Name = "Junior", Time = "17:00", Weekday = DayOfWeek.Monday };
        var data = new AppData { Exercises = [selectedExercise, otherExercise] };
        var participants = new ParticipantRepository(data);
        var attendance = new AttendanceRepository(data);
        var participant = new Participant
        {
            ExerciseId = selectedExercise.Id,
            FirstName = "Ada",
            Surname = "Lind",
            PersonalNumber = "200101011234",
            IsTrainer = false
        };

        participants.Add(participant);
        attendance.SetAttendance(selectedExercise.Id, participant.Id, new DateOnly(2026, 9, 8), true);
        attendance.SetAttendance(selectedExercise.Id, participant.Id, new DateOnly(2026, 9, 8), false);

        Assert.AreEqual(1, participants.ListForExercise(selectedExercise.Id).Count);
        Assert.AreEqual(0, participants.ListForExercise(otherExercise.Id).Count);
        Assert.AreEqual("010101", participants.Get(participant.Id)!.DisplayBirthDate);
        Assert.IsFalse(attendance.GetForSession(selectedExercise.Id, new DateOnly(2026, 9, 8)).Single().IsPresent);
    }
}
