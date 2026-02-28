namespace AgenticBlazer.Services;

public class UserState
{
    public Guid? CurrentUserId { get; private set; }
    public string? CurrentUsername { get; private set; }
    public string CurrentTheme { get; private set; } = "light";
    
    public event Action? OnChange;
    
    public bool IsLoggedIn => CurrentUserId.HasValue;

    public void Login(Guid userId, string username, string theme = "light")
    {
        CurrentUserId = userId;
        CurrentUsername = username;
        CurrentTheme = string.IsNullOrEmpty(theme) ? "light" : theme;
        OnChange?.Invoke();
    }

    public void SetTheme(string theme)
    {
        CurrentTheme = theme;
        OnChange?.Invoke();
    }

    public void Logout()
    {
        CurrentUserId = null;
        CurrentUsername = null;
        // Do not reset CurrentTheme so it persists on the login screen
        OnChange?.Invoke();
    }
}
