namespace AgenticBlazer.Services;

public class UserState
{
    public Guid? CurrentUserId { get; private set; }
    public string? CurrentUsername { get; private set; }
    
    public bool IsLoggedIn => CurrentUserId.HasValue;

    public void Login(Guid userId, string username)
    {
        CurrentUserId = userId;
        CurrentUsername = username;
    }

    public void Logout()
    {
        CurrentUserId = null;
        CurrentUsername = null;
    }
}
