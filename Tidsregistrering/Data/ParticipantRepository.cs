using Tidsregistrering.Models;

namespace Tidsregistrering.Data;

/// <summary>Global participant operations with exercise memberships.</summary>
public sealed class ParticipantRepository(AppData data)
{
    public IReadOnlyList<Participant> ListForExercise(Guid exerciseId, bool includeArchived = false) => data.Participants
        .Where(participant => participant.ExerciseIds.Contains(exerciseId) || participant.ExerciseId == exerciseId)
        .Where(participant => includeArchived || !participant.IsArchived)
        .OrderBy(participant => participant.Surname, StringComparer.CurrentCultureIgnoreCase)
        .ThenBy(participant => participant.FirstName, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    public IReadOnlyList<Participant> ListAll(bool includeArchived = false) => data.Participants
        .Where(participant => includeArchived || !participant.IsArchived)
        .OrderBy(participant => participant.Surname, StringComparer.CurrentCultureIgnoreCase)
        .ThenBy(participant => participant.FirstName, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    public Participant? Get(Guid id) => data.Participants.FirstOrDefault(participant => participant.Id == id);

    public bool IsMember(Guid participantId, Guid exerciseId) => Get(participantId) is { } participant &&
        (participant.ExerciseIds.Contains(exerciseId) || participant.ExerciseId == exerciseId);

    public bool TryAdd(Participant participant, IEnumerable<Guid> exerciseIds)
    {
        var identity = IdentityKey(participant.PersonalNumber);
        if (identity is not null && data.Participants.Any(existing =>
                !existing.IsArchived && IdentityKey(existing.PersonalNumber) == identity))
        {
            return false;
        }

        var memberships = exerciseIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (memberships.Count == 0)
        {
            return false;
        }

        data.Participants.Add(Copy(participant, exerciseIds: memberships, isArchived: false, archivedAtUtc: null));
        return true;
    }

    public void Add(Participant participant)
    {
        var memberships = participant.ExerciseIds.Concat(
            participant.ExerciseId == Guid.Empty ? [] : [participant.ExerciseId]);
        if (!TryAdd(participant, memberships))
        {
            throw new InvalidOperationException("Participant already exists or has no exercise membership.");
        }
    }

    /// <summary>Updates global details and replaces the active exercise membership set.</summary>
    public bool UpdateAndSetExercises(
        Guid id,
        string firstName,
        string surname,
        string personalNumber,
        bool isTrainer,
        IEnumerable<Guid> exerciseIds)
    {
        var index = data.Participants.FindIndex(participant => participant.Id == id);
        if (index < 0 || data.Participants[index].IsArchived)
        {
            return false;
        }

        var identity = IdentityKey(personalNumber);
        if (identity is not null && data.Participants.Any(participant =>
                participant.Id != id && !participant.IsArchived && IdentityKey(participant.PersonalNumber) == identity))
        {
            return false;
        }

        var memberships = exerciseIds.Where(exerciseId => exerciseId != Guid.Empty).Distinct().ToList();
        if (memberships.Count == 0) return false;

        var existing = data.Participants[index];
        data.Participants[index] = Copy(existing, firstName, surname, personalNumber, isTrainer,
            memberships, false, null);
        return true;
    }

    /// <summary>Removes only this exercise membership; historic person/attendance data remains.</summary>
    public bool RemoveFromExercise(Guid id, Guid exerciseId)
    {
        var index = data.Participants.FindIndex(participant => participant.Id == id);
        if (index < 0 || data.Participants[index].IsArchived)
        {
            return false;
        }

        var participant = data.Participants[index];
        var memberships = participant.ExerciseIds
            .Concat(participant.ExerciseId == Guid.Empty ? [] : [participant.ExerciseId])
            .Where(candidate => candidate != exerciseId)
            .Distinct()
            .ToList();
        if (memberships.Count == participant.ExerciseIds.Count && participant.ExerciseId != exerciseId) return false;

        var archived = memberships.Count == 0;
        data.Participants[index] = Copy(participant, exerciseIds: memberships, isArchived: archived,
            archivedAtUtc: archived ? DateTimeOffset.UtcNow : null);
        return true;
    }

    internal static string? IdentityKey(string personalNumber)
    {
        var value = personalNumber.Trim();
        return value.Length == 12 && value.All(char.IsDigit) ? value : null;
    }

    private static Participant Copy(
        Participant source,
        string? firstName = null,
        string? surname = null,
        string? personalNumber = null,
        bool? isTrainer = null,
        List<Guid>? exerciseIds = null,
        bool? isArchived = null,
        DateTimeOffset? archivedAtUtc = null) => new()
    {
        Id = source.Id,
        ExerciseIds = exerciseIds ?? source.ExerciseIds.ToList(),
        CreatedAtUtc = source.CreatedAtUtc,
        ArchivedAtUtc = archivedAtUtc,
        FirstName = firstName ?? source.FirstName,
        Surname = surname ?? source.Surname,
        PersonalNumber = personalNumber ?? source.PersonalNumber,
        IsTrainer = isTrainer ?? source.IsTrainer,
        IsArchived = isArchived ?? source.IsArchived
    };
}
