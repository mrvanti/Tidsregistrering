using Tidsregistrering.Models;

namespace Tidsregistrering.Data;

/// <summary>Raw attendance storage. It never deletes participant rosters.</summary>
public sealed class AttendanceRepository(AppData data)
{
    public AttendanceSession GetOrCreateSession(Guid exerciseId, DateOnly date)
    {
        var session = data.AttendanceSessions.FirstOrDefault(candidate => candidate.ExerciseId == exerciseId && candidate.Date == date);
        if (session is not null)
        {
            return session;
        }

        session = new AttendanceSession { ExerciseId = exerciseId, Date = date };
        data.AttendanceSessions.Add(session);
        return session;
    }

    public IReadOnlyList<AttendanceEntry> GetForSession(Guid exerciseId, DateOnly date) => data.AttendanceEntries
        .Where(entry => entry.ExerciseId == exerciseId && entry.Date == date)
        .ToList();

    public void SetAttendance(Guid exerciseId, Guid participantId, DateOnly date, bool isPresent)
    {
        var session = GetOrCreateSession(exerciseId, date);
        data.AttendanceEntries.RemoveAll(entry => entry.ExerciseId == exerciseId && entry.ParticipantId == participantId && entry.Date == date);
        data.AttendanceEntries.Add(new AttendanceEntry
        {
            SessionId = session.Id,
            ExerciseId = exerciseId,
            ParticipantId = participantId,
            Date = date,
            IsPresent = isPresent
        });
    }

    public IReadOnlyList<AttendanceEntry> GetRawAttendance(Guid exerciseId, DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new ArgumentException("Start date must not be after end date.", nameof(from));
        }

        return data.AttendanceEntries
            .Where(entry => entry.ExerciseId == exerciseId && entry.Date >= from && entry.Date <= to)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.ParticipantId)
            .ToList();
    }

    public int ClearAll()
    {
        var count = data.AttendanceEntries.Count;
        data.AttendanceEntries.Clear();
        data.AttendanceSessions.Clear();
        return count;
    }
}
