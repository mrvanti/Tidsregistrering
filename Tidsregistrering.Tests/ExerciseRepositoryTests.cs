using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Tests;

[TestClass]
public sealed class ExerciseRepositoryTests
{
    [TestMethod]
    public void ListSorted_OrdersByTimeThenWeekdayThenName()
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

        CollectionAssert.AreEqual(new[] { "Lunch", "Alpha", "Zebra", "Tuesday" }, names);
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
}
