using IRacingLeague.Data;
using IRacingLeague.Models;

namespace IRacingLeague.Business;

public class ResultService : IResultService
{
    private readonly IResultRepository _results;
    private readonly IRegistrationRepository _registrations;
    private readonly IUserRepository _users;

    public ResultService(IResultRepository results, IRegistrationRepository registrations, IUserRepository users)
    {
        _results = results;
        _registrations = registrations;
        _users = users;
    }

    public Result ApplyResult(Registration registration, Race race, Result result)
    {
        result.RegistrationId = registration.RegistrationId;
        result.RaceId = race.RaceId;

        var user = _users.Get(registration.UserId)
            ?? throw new KeyNotFoundException($"User with id {registration.UserId} not found.");

        // A driver can only have one result per race — re-entering one edits the
        // existing record in place rather than creating a duplicate, backing out
        // its previously-applied points/stats before applying the new ones.
        var existing = _results.GetAll()
            .FirstOrDefault(r => r.RegistrationId == registration.RegistrationId && r.RaceId == race.RaceId);

        if (existing != null)
        {
            registration.Points += result.Points - existing.Points;
            user.UndoRaceOutcome(existing.Position, existing.IncidentPoints);

            existing.Position = result.Position;
            existing.FastestLapSeconds = result.FastestLapSeconds;
            existing.Points = result.Points;
            existing.IncidentPoints = result.IncidentPoints;
            existing.Dnf = result.Dnf;
            existing.Notes = result.Notes;
            existing.FinishedAt = result.FinishedAt;
            result = existing;

            _results.Update(result);
        }
        else
        {
            registration.AddPoints(result.Points);
            _results.Add(result);
        }
        _results.SaveChanges();

        user.ApplyRaceOutcome(result.Position, result.IncidentPoints);
        _users.Update(user);
        _users.SaveChanges();

        _registrations.Update(registration);
        _registrations.SaveChanges();

        return result;
    }
}
