namespace Tidsregistrering.Models;

public sealed class Participant
{
    public Guid Id { get; init; } = Guid.NewGuid();
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
