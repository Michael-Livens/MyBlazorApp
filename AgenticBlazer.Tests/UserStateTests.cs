using System;
using Xunit;
using AgenticBlazer.Services;

namespace AgenticBlazer.Tests
{
    public class UserStateTests
    {
        [Fact]
        public void Login_SetsUserPropertiesAndTheme()
        {
            var userState = new UserState();
            var userId = Guid.NewGuid();
            var eventFired = false;
            
            userState.OnChange += () => eventFired = true;

            userState.Login(userId, "TestUser", "dark");

            Assert.True(userState.IsLoggedIn);
            Assert.Equal(userId, userState.CurrentUserId);
            Assert.Equal("TestUser", userState.CurrentUsername);
            Assert.Equal("dark", userState.CurrentTheme);
            Assert.True(eventFired);
        }

        [Fact]
        public void Logout_ClearsUserPropertiesButKeepsTheme()
        {
            var userState = new UserState();
            var userId = Guid.NewGuid();
            userState.Login(userId, "TestUser", "oxidized");
            
            var eventFired = false;
            userState.OnChange += () => eventFired = true;

            userState.Logout();

            Assert.False(userState.IsLoggedIn);
            Assert.Null(userState.CurrentUserId);
            Assert.Null(userState.CurrentUsername);
            Assert.Equal("oxidized", userState.CurrentTheme); // Theme remains
            Assert.True(eventFired);
        }

        [Fact]
        public void SetTheme_UpdatesThemeAndFiresEvent()
        {
            var userState = new UserState();
            var eventFired = false;
            userState.OnChange += () => eventFired = true;

            userState.SetTheme("dark");

            Assert.Equal("dark", userState.CurrentTheme);
            Assert.True(eventFired);
        }
    }
}
