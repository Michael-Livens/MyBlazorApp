using Microsoft.EntityFrameworkCore;

namespace AgenticBlazer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Poem> Poems => Set<Poem>();
    public DbSet<PoemUpvote> PoemUpvotes => Set<PoemUpvote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Prevent multiple cascade paths error in SQL Server
        modelBuilder.Entity<PoemUpvote>()
            .HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PoemUpvote>()
            .HasOne(u => u.Poem)
            .WithMany(p => p.Upvotes)
            .HasForeignKey(u => u.PoemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
