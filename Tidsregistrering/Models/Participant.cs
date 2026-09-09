using System.Text.Json.Serialization;

namespace Tidsregistrering.Models;

public sealed class Participant
{
    public Guid Id { get; init; } = Guid.NewGuid();
    /// <summary>Active exercise memberships for this globally unique person.</summary>
    public List<Guid> ExerciseIds { get; init; } = [];

    /// <summary>Legacy schema-v1 roster owner. Read during migration, never written when empty.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Guid ExerciseId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ArchivedAtUtc { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string Surname { get; init; } = string.Empty;
    public string PersonalNumber { get; init; } = string.Empty;
    public bool IsTrainer { get; init; }
    public bool IsArchived { get; init; }

    public string DisplayBirthDate =>
        PersonalNumber.Length == 12 && PersonalNumber.All(char.IsDigit) ? PersonalNumber[2..8] : string.Empty;

    public override string ToString() => string.IsNullOrEmpty(DisplayBirthDate)
        ? $"{FirstName} {Surname}"
        : $"{FirstName} {Surname}  {DisplayBirthDate}";
}
