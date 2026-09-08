using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Tests;

[TestClass]
public sealed class LokGroupAllocationEngineTests
{
    [TestMethod]
    public void Allocate_TwoLeadersAndSixParticipantsCreatesTwoOnePlusThreeGroups()
    {
        var result = Allocate(2, 6);

        Assert.AreEqual(2, result.Groups.Count);
        Assert.IsTrue(result.Groups.All(group => group.LeaderIds.Count == 1 && group.ParticipantIds.Count == 3));
    }

    [TestMethod]
    public void Allocate_ReturnsNoGroupsWithoutLeaderOrThreeParticipants()
    {
        Assert.AreEqual(0, Allocate(0, 6).Groups.Count);
        var result = Allocate(1, 2);
        Assert.AreEqual(0, result.Groups.Count);
        Assert.AreEqual(1, result.UnassignedLeaderIds.Count);
        Assert.AreEqual(2, result.UnassignedParticipantIds.Count);
    }

    [TestMethod]
    public void Allocate_DistributesSurplusParticipantsBeforeSecondLeader()
    {
        var result = Allocate(3, 7);

        Assert.AreEqual(2, result.Groups.Count);
        Assert.AreEqual(2, result.Groups[0].LeaderIds.Count);
        Assert.AreEqual(1, result.Groups[1].LeaderIds.Count);
        Assert.AreEqual(4, result.Groups[0].ParticipantIds.Count);
        Assert.AreEqual(3, result.Groups[1].ParticipantIds.Count);
    }

    [TestMethod]
    public void Allocate_IsDeterministicAndKeepsSurplusLeadersExplicit()
    {
        var assessments = CreateAssessments(5, 3).Reverse().ToList();
        var engine = new LokGroupAllocationEngine();
        var first = engine.Allocate(Guid.Empty, new DateOnly(2026, 9, 8), assessments);
        var second = engine.Allocate(Guid.Empty, new DateOnly(2026, 9, 8), assessments);

        CollectionAssert.AreEqual(first.Groups.SelectMany(group => group.LeaderIds).ToList(), second.Groups.SelectMany(group => group.LeaderIds).ToList());
        Assert.AreEqual(3, first.UnassignedLeaderIds.Count);
    }

    private static ExportGroupingResult Allocate(int leaderCount, int participantCount) => new LokGroupAllocationEngine().Allocate(
        Guid.Empty,
        new DateOnly(2026, 9, 8),
        CreateAssessments(leaderCount, participantCount));

    private static IEnumerable<LokEligibilityResult> CreateAssessments(int leaderCount, int participantCount) => Enumerable.Range(0, leaderCount + participantCount)
        .Select(index => new LokEligibilityResult { ParticipantId = new Guid(index + 1, 0, 0, new byte[8]), IsEligible = true, IsLeader = index < leaderCount });
}
