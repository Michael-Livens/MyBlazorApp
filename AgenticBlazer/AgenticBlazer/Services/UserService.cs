using AgenticBlazer.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenticBlazer.Services;

public class UserService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public UserService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> RegisterAsync(string username, string password, string theme = "light")
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        var userExists = await db.Users.AnyAsync(u => u.Username == username);
        if (userExists)
        {
            return false;
        }

        db.Users.Add(new User { Username = username, Password = password, Theme = theme });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<User?> ValidateLoginAsync(string username, string password)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Users.FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
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

