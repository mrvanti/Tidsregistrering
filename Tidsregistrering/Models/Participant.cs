namespace Tidsregistrering.Models;

public sealed class Participant
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ExerciseId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string Surname { get; init; } = string.Empty;
    public string PersonalNumber { get; init; } = string.Empty;
    public bool IsTrainer { get; init; }

    public string DisplayBirthDate =>
        PersonalNumber.Length >= 8 ? PersonalNumber[2..8] : PersonalNumber;

    public override string ToString() => $"{FirstName} {Surname}  {DisplayBirthDate}";
}
