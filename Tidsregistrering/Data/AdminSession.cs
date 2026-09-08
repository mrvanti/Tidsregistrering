namespace Tidsregistrering.Data;

/// <summary>Local admin access with an inactivity expiry.</summary>
public sealed class AdminSession
{
    private readonly string pin;
    private readonly Func<DateTimeOffset> utcNow;
    private DateTimeOffset? expiresAtUtc;

    public AdminSession(string pin, Func<DateTimeOffset>? utcNow = null)
    {
        this.pin = pin;
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public bool IsActive => expiresAtUtc is { } expiry && utcNow() < expiry;

    public bool TrySignIn(string enteredPin)
    {
        if (!string.Equals(pin, enteredPin, StringComparison.Ordinal))
        {
            return false;
        }

        expiresAtUtc = utcNow().AddMinutes(3);
        return true;
    }

    public void RecordActivity()
    {
        if (expiresAtUtc is null) return;

        expiresAtUtc = utcNow().AddMinutes(3);
    }

    public void End() => expiresAtUtc = null;
}
