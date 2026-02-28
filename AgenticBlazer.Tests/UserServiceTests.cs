using System;
using System.Threading.Tasks;
using AgenticBlazer.Data;
using AgenticBlazer.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgenticBlazer.Tests
{
    public class UserServiceTests
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
        public async Task RegisterAsync_CreatesUser_WithTheme()
        {
            var options = GetOptions(Guid.NewGuid().ToString());
            var factory = new TestDbContextFactory(options);
            var service = new UserService(factory);

            var result = await service.RegisterAsync("user1", "pass123", "dark");
            
            Assert.True(result);
            using var db = factory.CreateDbContext();
            var user = await db.Users.SingleOrDefaultAsync(u => u.Username == "user1");
            Assert.NotNull(user);
            Assert.Equal("pass123", user.Password);
            Assert.Equal("dark", user.Theme);
        }

        [Fact]
        public async Task RegisterAsync_FailsIfUsernameExists()
        {
            var options = GetOptions(Guid.NewGuid().ToString());
            var factory = new TestDbContextFactory(options);
            var service = new UserService(factory);

            await service.RegisterAsync("user1", "pass123");
            var result = await service.RegisterAsync("user1", "newpass"); // username exists
            
            Assert.False(result);
            using var db = factory.CreateDbContext();
            Assert.Equal(1, await db.Users.CountAsync());
        }

        [Fact]
        public async Task ValidateLoginAsync_ReturnsUser_WhenCredentialsAreValid()
        {
            var options = GetOptions(Guid.NewGuid().ToString());
            var factory = new TestDbContextFactory(options);
            var service = new UserService(factory);

            await service.RegisterAsync("testuser", "securepass", "oxidized");

            var user = await service.ValidateLoginAsync("testuser", "securepass");
            
            Assert.NotNull(user);
            Assert.Equal("testuser", user.Username);
            Assert.Equal("oxidized", user.Theme);
        }

        [Fact]
        public async Task ValidateLoginAsync_ReturnsNull_WhenCredentialsAreInvalid()
        {
            var options = GetOptions(Guid.NewGuid().ToString());
            var factory = new TestDbContextFactory(options);
            var service = new UserService(factory);

            await service.RegisterAsync("testuser", "securepass");

            var user1 = await service.ValidateLoginAsync("wronguser", "securepass");
            var user2 = await service.ValidateLoginAsync("testuser", "wrongpass");
            
            Assert.Null(user1);
            Assert.Null(user2);
        }

        [Fact]
        public async Task UpdateThemeAsync_UpdatesUserTheme()
        {
            var options = GetOptions(Guid.NewGuid().ToString());
            var factory = new TestDbContextFactory(options);
            var service = new UserService(factory);

            using (var db = factory.CreateDbContext())
            {
                db.Users.Add(new User { Id = Guid.Empty, Username = "u1", Password = "p1", Theme = "light" });
                await db.SaveChangesAsync();
            }

            var userId = (await factory.CreateDbContext().Users.FirstAsync()).Id;

            await service.UpdateThemeAsync(userId, "dark");

            using (var db = factory.CreateDbContext())
            {
                var user = await db.Users.FindAsync(userId);
                Assert.Equal("dark", user.Theme);
            }
        }
    }
}