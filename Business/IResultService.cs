using IRacingLeague.Models;

namespace IRacingLeague.Business;

public interface IResultService
{
    Result ApplyResult(Registration registration, Race race, Result result);
}
