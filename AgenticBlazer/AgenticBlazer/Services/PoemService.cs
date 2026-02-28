using AgenticBlazer.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenticBlazer.Services;

public class PoemService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public PoemService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<Poem>> GetSubmittedPoemsAsync(string orderBy = "latest")
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        var query = db.Poems
            .Include(p => p.User)
            .Include(p => p.Upvotes)
            .Where(p => p.IsSubmitted);

        if (orderBy == "upvotes")
        {
            return await query
                .OrderByDescending(p => p.Upvotes.Count)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        
        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Poem>> GetUserPoemsAsync(Guid userId)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Poems
            .Include(p => p.Upvotes)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Poem?> GetPoemAsync(int id)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Poems.FindAsync(id);
    }

    public async Task AddPoemAsync(Poem poem)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        db.Poems.Add(poem);
        await db.SaveChangesAsync();
    }

    public async Task UpdatePoemAsync(Poem poem)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        db.Poems.Update(poem);
        await db.SaveChangesAsync();
    }

    public async Task DeletePoemAsync(int id)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        var poem = await db.Poems.FindAsync(id);
        if (poem != null)
        {
            db.Poems.Remove(poem);
            await db.SaveChangesAsync();
        }
    }

    public async Task ToggleUpvoteAsync(int poemId, Guid userId)
    {
        using var db = await _dbContextFactory.CreateDbContextAsync();
        var existing = await db.PoemUpvotes
            .FirstOrDefaultAsync(u => u.PoemId == poemId && u.UserId == userId);
            
        if (existing != null)
        {
            db.PoemUpvotes.Remove(existing);
        }
        else
        {
            db.PoemUpvotes.Add(new PoemUpvote { PoemId = poemId, UserId = userId });
        }
        
        await db.SaveChangesAsync();
    }
}
