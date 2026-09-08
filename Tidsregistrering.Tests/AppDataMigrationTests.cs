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
}
