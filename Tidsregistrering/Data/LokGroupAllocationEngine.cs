using Tidsregistrering.Models;

namespace Tidsregistrering.Data;

public sealed class LokGroupAllocationEngine
{
    public ExportGroupingResult Allocate(Guid exerciseId, DateOnly date, IEnumerable<LokEligibilityResult> assessments)
    {
        var eligible = assessments.Where(assessment => assessment.IsEligible).OrderBy(assessment => assessment.ParticipantId).ToList();
        var leaders = eligible.Where(assessment => assessment.IsLeader).Select(assessment => assessment.ParticipantId).ToList();
        var participants = eligible.Where(assessment => !assessment.IsLeader).Select(assessment => assessment.ParticipantId).ToList();
        var groupCount = Math.Min(leaders.Count, participants.Count / 3);
        if (groupCount == 0)
        {
            return new ExportGroupingResult
            {
                ExerciseId = exerciseId,
                Date = date,
                UnassignedLeaderIds = leaders,
                UnassignedParticipantIds = participants
            };
        }

        var groups = Enumerable.Range(0, groupCount)
            .Select(index => new ExportGroup
            {
                Number = index + 1,
                LeaderIds = [leaders[index]],
                ParticipantIds = participants.Skip(index * 3).Take(3).ToList()
            })
            .ToList();

        foreach (var (participantId, index) in participants.Skip(groupCount * 3).Select((participantId, index) => (participantId, index)))
        {
            groups[index % groupCount].ParticipantIds.Add(participantId);
        }

        var remainingLeaders = leaders.Skip(groupCount).ToList();
        for (var index = 0; index < Math.Min(remainingLeaders.Count, groupCount); index++)
        {
            groups[index].LeaderIds.Add(remainingLeaders[index]);
        }

        return new ExportGroupingResult
        {
            ExerciseId = exerciseId,
            Date = date,
            Groups = groups,
            UnassignedLeaderIds = remainingLeaders.Skip(groupCount).ToList()
        };
    }
}
