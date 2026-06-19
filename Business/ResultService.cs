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
            user.UndoRaceOutcome(existing.Position, existing.IncidentPoints, existing.LapsCompleted);

            existing.Position = result.Position;
            existing.FastestLapSeconds = result.FastestLapSeconds;
            existing.Points = result.Points;
            existing.IncidentPoints = result.IncidentPoints;
            existing.LapsCompleted = result.LapsCompleted;
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

        user.ApplyRaceOutcome(result.Position, result.IncidentPoints, result.LapsCompleted);
        _users.Update(user);
        _users.SaveChanges();

        _registrations.Update(registration);
        _registrations.SaveChanges();

        return result;
    }

    public Result GetById(int id)
    {
        var result = _results.Get(id);
        if (result == null)
            throw new KeyNotFoundException($"Result with id {id} not found.");
        return result;
    }

    public IEnumerable<Result> GetByRace(int raceId) =>
        _results.GetAll().Where(r => r.RaceId == raceId).ToList();

    public void Delete(int resultId)
    {
        var result = _results.Get(resultId)
            ?? throw new KeyNotFoundException($"Result with id {resultId} not found.");
        RevertEffects(result);
        _results.Delete(resultId);
        _results.SaveChanges();
    }

    public void DeleteByRace(int raceId) =>
        DeleteWhere(r => r.RaceId == raceId);

    public void DeleteByRegistration(int registrationId) =>
        DeleteWhere(r => r.RegistrationId == registrationId);

    private void DeleteWhere(Func<Result, bool> predicate)
    {
        var doomed = _results.GetAll().Where(predicate).ToList();
        foreach (var result in doomed)
        {
            RevertEffects(result);
            _results.Delete(result.ResultId);
        }
        if (doomed.Count > 0)
            _results.SaveChanges();
    }

    // Undo the standings points and global driver stats a result applied — the exact
    // inverse of ApplyResult — so removing a result can't leave inflated points or
    // stats behind. The parents are null-checked: when a result is dropped as part
    // of deleting its registration or its driver, that parent may already be gone.
    private void RevertEffects(Result result)
    {
        var registration = _registrations.Get(result.RegistrationId);
        if (registration == null)
            return;

        registration.Points -= result.Points;
        _registrations.Update(registration);
        _registrations.SaveChanges();

        var user = _users.Get(registration.UserId);
        if (user == null)
            return;

        user.UndoRaceOutcome(result.Position, result.IncidentPoints, result.LapsCompleted);
        _users.Update(user);
        _users.SaveChanges();
    }
}
