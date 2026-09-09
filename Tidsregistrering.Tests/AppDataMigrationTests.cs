using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Tests;

[TestClass]
public sealed class AppDataMigrationTests
{
    [TestMethod]
    public void Migrate_AddsSessionAndLinksLegacyAttendanceEntry()
    {
        var exerciseId = Guid.NewGuid();
        var entry = new AttendanceEntry
        {
            ExerciseId = exerciseId,
            ParticipantId = Guid.NewGuid(),
            Date = new DateOnly(2026, 9, 8),
            IsPresent = true
        };

        var migrated = AppDataMigration.Migrate(new AppData { AttendanceEntries = [entry] });

        Assert.AreEqual(AppDataMigration.CurrentSchemaVersion, migrated.SchemaVersion);
        Assert.AreEqual(1, migrated.AttendanceSessions.Count);
        Assert.AreEqual(migrated.AttendanceSessions[0].Id, migrated.AttendanceEntries[0].SessionId);
    }

    [TestMethod]
    public void Migrate_NormalizesMalformedNullCollections()
    {
        var migrated = AppDataMigration.Migrate(new AppData
        {
            SchemaVersion = AppDataMigration.CurrentSchemaVersion,
            Exercises = null!,
            Participants = null!,
            AttendanceSessions = null!,
            AttendanceEntries = null!
        });

        Assert.AreEqual(0, migrated.Exercises.Count);
        Assert.AreEqual(0, migrated.Participants.Count);
        Assert.AreEqual(0, migrated.AttendanceSessions.Count);
        Assert.AreEqual(0, migrated.AttendanceEntries.Count);
    }

    [TestMethod]
    public void Migrate_DeduplicatesLegacySessionsForSameExerciseAndDate()
    {
        var exerciseId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 8);
        var first = new AttendanceSession { ExerciseId = exerciseId, Date = date };
        var data = new AppData
        {
            AttendanceSessions = [first, new AttendanceSession { ExerciseId = exerciseId, Date = date }],
            AttendanceEntries = [new AttendanceEntry { ExerciseId = exerciseId, ParticipantId = Guid.NewGuid(), Date = date }]
        };

        var migrated = AppDataMigration.Migrate(data);

        Assert.AreEqual(1, migrated.AttendanceSessions.Count);
        Assert.AreEqual(first.Id, migrated.AttendanceEntries[0].SessionId);
    }

    [TestMethod]
    public void Migrate_MergesLegacyRosterCopiesAndRemapsAttendanceToCanonicalPerson()
    {
        var firstExercise = Guid.NewGuid();
        var secondExercise = Guid.NewGuid();
        var first = new Participant { ExerciseId = firstExercise, FirstName = "Ada", PersonalNumber = "200101011234" };
        var copy = new Participant { ExerciseId = secondExercise, FirstName = "Ada", PersonalNumber = "200101011234" };
        var entry = new AttendanceEntry
        {
            ExerciseId = secondExercise,
            ParticipantId = copy.Id,
            Date = new DateOnly(2026, 9, 8),
            IsPresent = true
        };

        var migrated = AppDataMigration.Migrate(new AppData { Participants = [first, copy], AttendanceEntries = [entry] });

        Assert.AreEqual(1, migrated.Participants.Count);
        CollectionAssert.AreEquivalent(new[] { firstExercise, secondExercise }, migrated.Participants[0].ExerciseIds);
        Assert.AreEqual(migrated.Participants[0].Id, migrated.AttendanceEntries.Single().ParticipantId);
    }

    [TestMethod]
    public void Migrate_DoesNotMergeBlankOrMalformedPersonalNumbers()
    {
        var migrated = AppDataMigration.Migrate(new AppData
        {
            Participants =
            [
                new Participant { ExerciseId = Guid.NewGuid(), PersonalNumber = "" },
                new Participant { ExerciseId = Guid.NewGuid(), PersonalNumber = "" },
                new Participant { ExerciseId = Guid.NewGuid(), PersonalNumber = "invalid" },
                new Participant { ExerciseId = Guid.NewGuid(), PersonalNumber = "invalid" }
            ]
        });

        Assert.AreEqual(4, migrated.Participants.Count);
    }
}
