using Tidsregistrering.Models;

namespace Tidsregistrering.Data;

/// <summary>Exercise-owned participant operations. Removal archives historic data.</summary>
public sealed class ParticipantRepository(AppData data)
{
    public IReadOnlyList<Participant> ListForExercise(Guid exerciseId, bool includeArchived = false) => data.Participants
        .Where(participant => participant.ExerciseId == exerciseId)
        .Where(participant => includeArchived || !participant.IsArchived)
        .OrderBy(participant => participant.Surname, StringComparer.CurrentCultureIgnoreCase)
        .ThenBy(participant => participant.FirstName, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    public Participant? Get(Guid id) => data.Participants.FirstOrDefault(participant => participant.Id == id);

    public void Add(Participant participant) => data.Participants.Add(participant);

    public bool Archive(Guid id)
    {
        var index = data.Participants.FindIndex(participant => participant.Id == id);
        if (index < 0 || data.Participants[index].IsArchived)
        {
            return false;
        }

        var participant = data.Participants[index];
        data.Participants[index] = new Participant
        {
            Id = participant.Id,
            ExerciseId = participant.ExerciseId,
            CreatedAtUtc = participant.CreatedAtUtc,
            ArchivedAtUtc = DateTimeOffset.UtcNow,
            FirstName = participant.FirstName,
            Surname = participant.Surname,
            PersonalNumber = participant.PersonalNumber,
            IsTrainer = participant.IsTrainer,
            IsArchived = true
        };
        return true;
    }
}
