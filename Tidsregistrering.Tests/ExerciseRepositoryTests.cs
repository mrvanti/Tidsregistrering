using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Tests;

[TestClass]
public sealed class ExerciseRepositoryTests
{
    [TestMethod]
    public void ListSorted_OrdersByWeekdayThenTimeThenName()
    {
        var data = new AppData
        {
            Exercises =
            [
                new Exercise { Name = "Zebra", Time = "18:00", Weekday = DayOfWeek.Monday },
                new Exercise { Name = "Alpha", Time = "18:00", Weekday = DayOfWeek.Monday },
                new Exercise { Name = "Lunch", Time = "12:00", Weekday = DayOfWeek.Friday },
                new Exercise { Name = "Tuesday", Time = "18:00", Weekday = DayOfWeek.Tuesday }
            ]
        };

        var names = new ExerciseRepository(data).ListSorted().Select(exercise => exercise.Name).ToList();

        CollectionAssert.AreEqual(new[] { "Alpha", "Zebra", "Tuesday", "Lunch" }, names);
    }

    [TestMethod]
    public void TryAdd_RejectsDuplicateButAllowsDifferentExercise()
    {
        var data = new AppData();
        var repository = new ExerciseRepository(data);

        Assert.IsTrue(repository.TryAdd(new Exercise { Name = "Barn", Time = "18:00", Weekday = DayOfWeek.Monday }));
        Assert.IsFalse(repository.TryAdd(new Exercise { Name = "barn", Time = "18:00", Weekday = DayOfWeek.Monday }));
        Assert.IsTrue(repository.TryAdd(new Exercise { Name = "Barn", Time = "19:00", Weekday = DayOfWeek.Monday }));
        Assert.AreEqual(2, data.Exercises.Count);
    }

    [TestMethod]
    public void TryUpdate_PreservesIdentityAndRejectsAnotherExerciseDuplicate()
    {
        var first = new Exercise { Name = "First", Time = "18:00", Weekday = DayOfWeek.Monday };
        var second = new Exercise { Name = "Second", Time = "19:00", Weekday = DayOfWeek.Monday };
        var repository = new ExerciseRepository(new AppData { Exercises = [first, second] });

        Assert.IsTrue(repository.TryUpdate(first.Id, "Renamed", "17:00", DayOfWeek.Tuesday));
        var updated = repository.Get(first.Id)!;
        Assert.AreEqual(first.Id, updated.Id);
        Assert.AreEqual("Renamed", updated.Name);
        Assert.IsFalse(repository.TryUpdate(first.Id, second.Name, second.Time, second.Weekday));
    }

    [TestMethod]
    public void TryAddMany_AddsSameExerciseForEachSelectedWeekday()
    {
        var data = new AppData();
        var repository = new ExerciseRepository(data);

        Assert.IsTrue(repository.TryAddMany("Judo", "18:00", [DayOfWeek.Monday, DayOfWeek.Wednesday], out var first));

        Assert.IsNotNull(first);
        CollectionAssert.AreEquivalent(new[] { DayOfWeek.Monday, DayOfWeek.Wednesday }, data.Exercises.Select(exercise => exercise.Weekday).ToArray());
    }

    [TestMethod]
    public void TryUpdateAndAddWeekdays_PreservesSelectedExerciseAndAddsExtraWeekdays()
    {
        var existing = new Exercise { Name = "Judo", Time = "18:00", Weekday = DayOfWeek.Monday };
        var data = new AppData { Exercises = [existing] };
        var repository = new ExerciseRepository(data);

        Assert.IsTrue(repository.TryUpdateAndAddWeekdays(existing.Id, "Avancerad judo", "19:00", [DayOfWeek.Monday, DayOfWeek.Thursday], out var updated));

        Assert.IsNotNull(updated);
        Assert.AreEqual(existing.Id, updated.Id);
        Assert.AreEqual(DayOfWeek.Monday, repository.Get(existing.Id)!.Weekday);
        CollectionAssert.AreEquivalent(new[] { DayOfWeek.Monday, DayOfWeek.Thursday }, data.Exercises.Select(exercise => exercise.Weekday).ToArray());
    }

    [TestMethod]
    public void Delete_RemovesOnlyRequestedExercise()
    {
        var first = new Exercise { Name = "First" };
        var second = new Exercise { Name = "Second" };
        var data = new AppData { Exercises = [first, second] };
        var repository = new ExerciseRepository(data);

        Assert.IsTrue(repository.Delete(first.Id));
        Assert.AreEqual(second, repository.Get(second.Id));
        Assert.IsNull(repository.Get(first.Id));
    }

    [TestMethod]
    public void Delete_RemovesMembershipAndOwnedAttendanceButRetainsGlobalPeople()
    {
        var first = new Exercise { Name = "First" };
        var second = new Exercise { Name = "Second" };
        var data = new AppData
        {
            Exercises = [first, second],
            Participants = [new Participant { ExerciseIds = [first.Id] }, new Participant { ExerciseIds = [second.Id] }],
            AttendanceSessions = [new AttendanceSession { ExerciseId = first.Id }, new AttendanceSession { ExerciseId = second.Id }],
            AttendanceEntries = [new AttendanceEntry { ExerciseId = first.Id }, new AttendanceEntry { ExerciseId = second.Id }]
        };

        Assert.IsTrue(new ExerciseRepository(data).Delete(first.Id));

        Assert.AreEqual(2, data.Participants.Count);
        Assert.IsTrue(data.Participants.Single(participant => participant.ExerciseIds.Count == 0).IsArchived);
        Assert.AreEqual(second.Id, data.Participants.Single(participant => !participant.IsArchived).ExerciseIds.Single());
        Assert.AreEqual(1, data.AttendanceSessions.Count);
        Assert.AreEqual(second.Id, data.AttendanceSessions[0].ExerciseId);
        Assert.AreEqual(1, data.AttendanceEntries.Count);
        Assert.AreEqual(second.Id, data.AttendanceEntries[0].ExerciseId);
    }
}
