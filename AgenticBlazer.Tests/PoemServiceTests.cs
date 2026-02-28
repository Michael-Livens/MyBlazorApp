using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgenticBlazer.Data;
using AgenticBlazer.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgenticBlazer.Tests
{
    public class PoemServiceTests
    {
        private class TestDbContextFactory : IDbContextFactory<AppDbContext>
        {
            private readonly DbContextOptions<AppDbContext> _options;

            public TestDbContextFactory(DbContextOptions<AppDbContext> options)
            {
                _options = options;
            }

            public AppDbContext CreateDbContext()
            {
                return new AppDbContext(_options);
            }
        }

        private DbContextOptions<AppDbContext> GetOptions(string dbName)
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
        }

        [Fact]
        public async Task GetSubmittedPoemsAsync_PaginatesCorrectly()
        {
            // Arrange
            var options = GetOptions(Guid.NewGuid().ToString());
            var factory = new TestDbContextFactory(options);
            
            using (var db = factory.CreateDbContext())
            {
                var userId = Guid.NewGuid();
                db.Users.Add(new User { Id = userId, Username = "TestUser", Password = "TestPassword" });

                // Seed some poems
                for (int i = 1; i <= 40; i++)
                {
                    db.Poems.Add(new Poem
                    {
                        Id = i,
                        UserId = userId,
                        Title = $"Poem {i}",
                        Content = $"Content {i}",
                        IsSubmitted = true,
                        CreatedAt = DateTime.UtcNow.AddMinutes(i) // Ensure order
                    });
                }
                
                // Add one unsubmitted poem
                db.Poems.Add(new Poem
                {
                    Id = 41,
                    UserId = userId,
                    Title = "Unsubmitted",
                    Content = "Draft",
                    IsSubmitted = false,
                    CreatedAt = DateTime.UtcNow
                });
                
                await db.SaveChangesAsync();
            }

            var service = new PoemService(factory);

            // Act: Request 30 items starting from index 0
            // Should get items at index 0..29
            var count = await service.GetSubmittedPoemsCountAsync();
            var poems = await service.GetSubmittedPoemsAsync(startIndex: 0, count: 30, orderBy: "latest");

            // Assert
            Assert.Equal(40, count);
            Assert.Equal(30, poems.Count);
            
            // Verifying "latest" sort:
            // Highest CreatedAt is Poem 40 (index 0)
            Assert.Equal("Poem 40", poems[0].Title);
            Assert.Equal("Poem 39", poems[1].Title);
            Assert.Equal("Poem 38", poems[2].Title);
            Assert.Equal("Poem 37", poems[3].Title);
            Assert.Equal("Poem 11", poems[29].Title);
        }
        
        [Fact]
        public async Task GetSubmittedPoemsAsync_OrdersByUpvotes()
        {
            // Arrange
            var options = GetOptions(Guid.NewGuid().ToString());
            var factory = new TestDbContextFactory(options);
            
            using (var db = factory.CreateDbContext())
            {
                var userId = Guid.NewGuid();
                db.Users.Add(new User { Id = userId, Username = "TestUser", Password = "TestPassword" });

                var p1 = new Poem { Id = 1, UserId = userId, Title = "P1", IsSubmitted = true, CreatedAt = DateTime.UtcNow, Upvotes = new List<PoemUpvote>() };
                var p2 = new Poem { Id = 2, UserId = userId, Title = "P2", IsSubmitted = true, CreatedAt = DateTime.UtcNow, Upvotes = new List<PoemUpvote> { new PoemUpvote { Id = 1, UserId = Guid.NewGuid() } } };
                var p3 = new Poem { Id = 3, UserId = userId, Title = "P3", IsSubmitted = true, CreatedAt = DateTime.UtcNow, Upvotes = new List<PoemUpvote> { new PoemUpvote { Id = 2, UserId = Guid.NewGuid() }, new PoemUpvote { Id = 3, UserId = Guid.NewGuid() } } };

                db.Poems.AddRange(p1, p2, p3);
                await db.SaveChangesAsync();
            }

            var service = new PoemService(factory);

            // Act
            var poems = await service.GetSubmittedPoemsAsync(0, 3, "upvotes");

            // Assert
            Assert.Equal(3, poems.Count);
            Assert.Equal("P3", poems[0].Title); // 2 upvotes
            Assert.Equal("P2", poems[1].Title); // 1 upvote
            Assert.Equal("P1", poems[2].Title); // 0 upvotes
        }
    }
}