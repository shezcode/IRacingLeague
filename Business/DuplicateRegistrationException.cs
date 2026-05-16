namespace IRacingLeague.Business;

public class DuplicateRegistrationException : Exception
{
    public DuplicateRegistrationException(string message) : base(message) { }
}
