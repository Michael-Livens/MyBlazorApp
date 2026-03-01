using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using AgenticBlazer.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenticBlazer.Services;

public class UserService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly SecretClient _secretClient;

    public UserService(IDbContextFactory<AppDbContext> dbContextFactory, SecretClient secretClient)
    {
        _dbContextFactory = dbContextFactory;
        _secretClient = secretClient;
    }

    public async Task<bool> RegisterAsync(string username, string password, string theme = "light")
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        var userExists = await db.Users.AnyAsync(u => u.Username == username);
        if (userExists)
        {
            return false;
        }
        // Store password securely in Key Vault
        await _secretClient.SetSecretAsync($"user-{username}-password", password);
        db.Users.Add(new User { Username = username, Theme = theme });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ValidateLoginAsync(string username, string password)
    {
        KeyVaultSecret secret = await _secretClient.GetSecretAsync($"user-{username}-password");
        return secret.Value == password;
    }

    public async Task UpdateThemeAsync(Guid userId, string theme)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(userId);
        if (user != null)
        {
            user.Theme = theme;
            await db.SaveChangesAsync();
        }
    }
}


