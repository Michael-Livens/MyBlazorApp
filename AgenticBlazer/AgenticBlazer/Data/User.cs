namespace AgenticBlazer.Data;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    // Password property removed for secure credential migration
    public string Theme { get; set; } = "light";
}


