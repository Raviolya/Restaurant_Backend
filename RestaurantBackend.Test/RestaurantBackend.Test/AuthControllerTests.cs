using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using RestaurantBackend.Controllers;
using RestaurantBackend.Data;
using RestaurantBackend.DTOs;
using RestaurantBackend.Models;
using RestaurantBackend.Repositories.Interfaces;
using RestaurantBackend.Services;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace RestaurantBackend.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly RestaurantDbContext _dbContext;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _configurationMock = new Mock<IConfiguration>();
            _userRepositoryMock = new Mock<IUserRepository>();

            var options = new DbContextOptionsBuilder<RestaurantDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new RestaurantDbContext(options);

            _controller = new AuthController(_authServiceMock.Object, _configurationMock.Object, _userRepositoryMock.Object, _dbContext);

            var httpContext = new Mock<HttpContext>();
            var request = new Mock<HttpRequest>();
            var response = new Mock<HttpResponse>();
            var cookies = new Mock<IResponseCookies>();
            var requestCookies = new Mock<IRequestCookieCollection>();

            response.Setup(r => r.Cookies).Returns(cookies.Object);
            request.Setup(r => r.Cookies).Returns(requestCookies.Object);
            httpContext.Setup(c => c.Response).Returns(response.Object);
            httpContext.Setup(c => c.Request).Returns(request.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext.Object
            };
        }

        [Fact]
        public async Task Register_ValidUser_ReturnsCreatedResult()
        {
            var userDto = new CreateUserDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "1234567890",
                Password = "Password123",
                ConfirmPassword = "Password123",
                DateOfBirth = DateTime.UtcNow.AddYears(-20)
            };

            _userRepositoryMock.Setup(r => r.UserExists(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserModel>())).Returns(Task.CompletedTask);
            _userRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var customerRole = new RoleUserModel { Id = Guid.NewGuid(), Name = "Customer" };
            await _dbContext.RoleUsers.AddAsync(customerRole);
            await _dbContext.SaveChangesAsync();

            _authServiceMock.Setup(s => s.Login(It.IsAny<LoginDto>())).ReturnsAsync(new AuthResponse
            {
                Token = "jwt_token",
                RefreshToken = "refresh_token",
                Expiration = DateTime.UtcNow.AddMinutes(30),
                User = new UserResponseDto { Id = Guid.NewGuid(), Email = userDto.Email, Name = $"{userDto.FirstName} {userDto.LastName}", Role = "Customer" }
            });

            var result = await _controller.Register(userDto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(201, createdResult.StatusCode);
            var responseDto = Assert.IsType<UserResponseDto>(createdResult.Value);
            Assert.Equal(userDto.Email, responseDto?.Email);
            Assert.Equal("Customer", responseDto?.Role);
        }

        [Fact]
        public async Task Register_UserAlreadyExists_ReturnsConflict()
        {
            var userDto = new CreateUserDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "1234567890",
                Password = "Password123",
                ConfirmPassword = "Password123",
                DateOfBirth = DateTime.UtcNow.AddYears(-20)
            };

            _userRepositoryMock.Setup(r => r.UserExists(userDto.Email, userDto.Phone)).ReturnsAsync(true);

            var result = await _controller.Register(userDto);

            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(409, conflictResult.StatusCode);
            Assert.Equal("Пользователь с таким email или телефоном уже существует", conflictResult.Value);
        }

        [Fact]
        public async Task Register_UnderAgeUser_ReturnsBadRequest()
        {
            var userDto = new CreateUserDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "1234567890",
                Password = "Password123",
                ConfirmPassword = "Password123",
                DateOfBirth = DateTime.UtcNow.AddYears(-5)
            };

            var result = await _controller.Register(userDto);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Equal("Регистрация только для лиц старше 10 лет", badRequestResult.Value);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithTokens()
        {
            var loginDto = new LoginDto
            {
                Email = "john.doe@example.com",
                Password = "Password123"
            };

            _authServiceMock.Setup(s => s.Login(loginDto)).ReturnsAsync(new AuthResponse
            {
                Token = "jwt_token",
                RefreshToken = "refresh_token",
                Expiration = DateTime.UtcNow.AddMinutes(30)
            });

            _configurationMock.Setup(c => c["Jwt:CookieDomain"]).Returns("localhost");

            var result = await _controller.Login(loginDto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var authResponse = Assert.IsType<AuthResponse>(okResult.Value);
            _controller.HttpContext.Response.Cookies.Append("access_token", "jwt_token", It.IsAny<CookieOptions>());
            _controller.HttpContext.Response.Cookies.Append("refresh_token", "refresh_token", It.IsAny<CookieOptions>());
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            var loginDto = new LoginDto
            {
                Email = "john.doe@example.com",
                Password = "WrongPassword"
            };

            _authServiceMock.Setup(s => s.Login(loginDto)).ThrowsAsync(new UnauthorizedAccessException("Неверный email или пароль"));

            var result = await _controller.Login(loginDto);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            Assert.Equal("Неверный email или пароль", unauthorizedResult.Value);
        }

        [Fact]
        public async Task Refresh_ValidTokens_ReturnsOkWithNewTokens()
        {
            var requestCookies = new Mock<IRequestCookieCollection>();
            requestCookies.Setup(c => c["access_token"]).Returns("old_jwt_token");
            requestCookies.Setup(c => c["refresh_token"]).Returns("old_refresh_token");

            var responseCookies = new Mock<IResponseCookies>();
            var response = new Mock<HttpResponse>();
            response.Setup(r => r.Cookies).Returns(responseCookies.Object);

            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Cookies).Returns(requestCookies.Object);

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.Request).Returns(request.Object);
            httpContext.Setup(c => c.Response).Returns(response.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext.Object
            };

            _configurationMock.Setup(c => c["Jwt:CookieDomain"]).Returns("localhost");
            _configurationMock.Setup(c => c["Jwt:RefreshTokenExpirationDays"]).Returns("7");

            _authServiceMock.Setup(s => s.RefreshToken("old_jwt_token", "old_refresh_token"))
                .ReturnsAsync(new AuthResponse
                {
                    Token = "new_jwt_token",
                    RefreshToken = "new_refresh_token",
                    Expiration = DateTime.UtcNow.AddMinutes(30)
                });

            var result = await _controller.Refresh();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.IsType<AuthResponse>(okResult.Value);
            responseCookies.Verify(c => c.Append("access_token", "new_jwt_token", It.IsAny<CookieOptions>()), Times.Once());
            responseCookies.Verify(c => c.Append("refresh_token", "new_refresh_token", It.IsAny<CookieOptions>()), Times.Once());
        }

        [Fact]
        public async Task Refresh_InvalidTokens_ReturnsUnauthorized()
        {
            var requestCookies = new Mock<IRequestCookieCollection>();
            requestCookies.Setup(c => c["access_token"]).Returns("invalid_jwt_token");
            requestCookies.Setup(c => c["refresh_token"]).Returns("invalid_refresh_token");

            var responseCookies = new Mock<IResponseCookies>();
            var response = new Mock<HttpResponse>();
            response.Setup(r => r.Cookies).Returns(responseCookies.Object);

            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Cookies).Returns(requestCookies.Object);

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.Request).Returns(request.Object);
            httpContext.Setup(c => c.Response).Returns(response.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext.Object
            };

            _authServiceMock.Setup(s => s.RefreshToken(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new SecurityTokenException("Недействительный токен"));

            var result = await _controller.Refresh();

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            Assert.Contains("Недействительный токен", unauthorizedResult.Value?.ToString() ?? string.Empty);
            responseCookies.Verify(c => c.Delete("access_token"), Times.Once());
            responseCookies.Verify(c => c.Delete("refresh_token"), Times.Once());
        }

        [Fact]
        public async Task Logout_AuthenticatedUser_ReturnsOk()
        {
            var email = "john.doe@example.com";
            var claims = new List<Claim> { new Claim(ClaimTypes.Email, email) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var responseCookies = new Mock<IResponseCookies>();
            var response = new Mock<HttpResponse>();
            response.Setup(r => r.Cookies).Returns(responseCookies.Object);

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.User).Returns(claimsPrincipal);
            httpContext.Setup(c => c.Response).Returns(response.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext.Object
            };

            _authServiceMock.Setup(s => s.RevokeToken(email)).ReturnsAsync(true);

            var result = await _controller.Logout();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal("Вы успешно вышли из системы.", okResult.Value);
            responseCookies.Verify(c => c.Delete("access_token"), Times.Once());
            responseCookies.Verify(c => c.Delete("refresh_token"), Times.Once());
        }

        [Fact]
        public async Task Logout_UnauthenticatedUser_ReturnsUnauthorized()
        {
            var responseCookies = new Mock<IResponseCookies>();
            var response = new Mock<HttpResponse>();
            response.Setup(r => r.Cookies).Returns(responseCookies.Object);

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));
            httpContext.Setup(c => c.Response).Returns(response.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext.Object
            };

            var result = await _controller.Logout();

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            Assert.Contains("Не удалось определить пользователя", unauthorizedResult.Value?.ToString() ?? string.Empty);
            responseCookies.Verify(c => c.Delete("access_token"), Times.Never());
            responseCookies.Verify(c => c.Delete("refresh_token"), Times.Never());
        }
    }
}