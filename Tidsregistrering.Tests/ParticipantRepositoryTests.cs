using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Tests;

[TestClass]
public sealed class ParticipantRepositoryTests
{
    [TestMethod]
    public void ListForExercise_ReturnsOnlyActiveParticipantsForExerciseInNameOrder()
    {
        var exerciseId = Guid.NewGuid();
        var data = new AppData
        {
            Participants =
            [
                new Participant { ExerciseId = exerciseId, FirstName = "Zara", Surname = "Andersson" },
                new Participant { ExerciseId = exerciseId, FirstName = "Adam", Surname = "Andersson" },
                new Participant { ExerciseId = exerciseId, FirstName = "Hidden", IsArchived = true },
                new Participant { ExerciseId = Guid.NewGuid(), FirstName = "Other" }
            ]
        };

        var listed = new ParticipantRepository(data).ListForExercise(exerciseId);

        CollectionAssert.AreEqual(new[] { "Adam", "Zara" }, listed.Select(participant => participant.FirstName).ToList());
    }

    [TestMethod]
    public void Archive_HidesParticipantAndKeepsHistoricFields()
    {
        var participant = new Participant { FirstName = "Ada", PersonalNumber = "200101011234" };
        var data = new AppData { Participants = [participant] };
        var repository = new ParticipantRepository(data);

        Assert.IsTrue(repository.Archive(participant.Id));
        var archived = repository.Get(participant.Id)!;
        Assert.IsTrue(archived.IsArchived);
        Assert.IsNotNull(archived.ArchivedAtUtc);
        Assert.AreEqual(participant.PersonalNumber, archived.PersonalNumber);
    }
}
