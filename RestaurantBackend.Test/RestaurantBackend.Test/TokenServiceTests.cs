using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using RestaurantBackend.Models;
using RestaurantBackend.Services;
using Xunit;

namespace RestaurantBackend.Tests
{
    public class TokenServiceTests
    {
        [Fact]
        public void GenerateAccessToken_ReturnsToken()
        {
            var inMemorySettings = new Dictionary<string, string> {
                {"Jwt:Key", "supersecretkey12345678901234567890"},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"},
                {"Jwt:AccessTokenExpirationMinutes", "60"}
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
            var user = new UserModel { Id = Guid.NewGuid(), Email = "test@example.com", Role = new RoleUserModel { Name = "Admin" } };
            var service = new TokenService(configuration);
            var token = service.GenerateAccessToken(user);
            Assert.False(string.IsNullOrEmpty(token));
        }

        [Fact]
        public void GenerateRefreshToken_ReturnsToken()
        {
            var configuration = new ConfigurationBuilder().Build();
            var service = new TokenService(configuration);
            var token = service.GenerateRefreshToken();
            Assert.False(string.IsNullOrEmpty(token));
        }
    }
} 