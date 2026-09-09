using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Tests;

[TestClass]
public sealed class ParticipantRepositoryTests
{
    [TestMethod]
    public void UpdateAndSetExercises_UpdatesOnePersonAndReplacesMemberships()
    {
        var firstExercise = Guid.NewGuid();
        var secondExercise = Guid.NewGuid();
        var participant = new Participant
        {
            ExerciseIds = [firstExercise],
            FirstName = "Ada",
            Surname = "Old",
            PersonalNumber = "200101011234"
        };
        var repository = new ParticipantRepository(new AppData { Participants = [participant] });

        Assert.IsTrue(repository.UpdateAndSetExercises(
            participant.Id, "Ada", "New", "200101011234", true, [secondExercise, secondExercise]));

        var updated = repository.Get(participant.Id)!;
        Assert.AreEqual("New", updated.Surname);
        Assert.IsTrue(updated.IsTrainer);
        Assert.AreEqual(0, repository.ListForExercise(firstExercise).Count);
        Assert.AreEqual(participant.Id, repository.ListForExercise(secondExercise).Single().Id);
    }
    [TestMethod]
    public void ListForExercise_ReturnsOnlyActiveParticipantsForExerciseInNameOrder()
    {
        var exerciseId = Guid.NewGuid();
        var data = new AppData
        {
            Participants =
            [
                new Participant { ExerciseIds = [exerciseId], FirstName = "Zara", Surname = "Andersson" },
                new Participant { ExerciseIds = [exerciseId], FirstName = "Adam", Surname = "Andersson" },
                new Participant { ExerciseIds = [exerciseId], FirstName = "Hidden", IsArchived = true },
                new Participant { ExerciseIds = [Guid.NewGuid()], FirstName = "Other" }
            ]
        };

        var listed = new ParticipantRepository(data).ListForExercise(exerciseId);

        CollectionAssert.AreEqual(new[] { "Adam", "Zara" }, listed.Select(participant => participant.FirstName).ToList());
    }

    [TestMethod]
    public void RemoveFromExercise_OnlyRemovesThatMembershipAndKeepsIdentity()
    {
        var firstExercise = Guid.NewGuid();
        var secondExercise = Guid.NewGuid();
        var participant = new Participant { ExerciseIds = [firstExercise, secondExercise], FirstName = "Ada", PersonalNumber = "200101011234" };
        var data = new AppData { Participants = [participant] };
        var repository = new ParticipantRepository(data);

        Assert.IsTrue(repository.RemoveFromExercise(participant.Id, firstExercise));
        var updated = repository.Get(participant.Id)!;
        Assert.IsFalse(updated.IsArchived);
        Assert.AreEqual(0, repository.ListForExercise(firstExercise).Count);
        Assert.AreEqual(participant.Id, repository.ListForExercise(secondExercise).Single().Id);
        Assert.AreEqual(participant.PersonalNumber, updated.PersonalNumber);
    }

    [TestMethod]
    public void TryAdd_RejectsSecondActivePersonWithSameValidPersonalNumber()
    {
        var data = new AppData();
        var repository = new ParticipantRepository(data);

        Assert.IsTrue(repository.TryAdd(new Participant { PersonalNumber = "200101011234" }, [Guid.NewGuid()]));
        Assert.IsFalse(repository.TryAdd(new Participant { PersonalNumber = "200101011234" }, [Guid.NewGuid()]));
        Assert.AreEqual(1, data.Participants.Count);
    }
}
