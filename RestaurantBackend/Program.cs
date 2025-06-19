using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.OpenApi.Models;
using RestaurantBackend.Data;
using RestaurantBackend.Repositories;
using RestaurantBackend.Repositories.Interfaces;
using RestaurantBackend.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5173",
                           "https://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ���������� DbContext
if (builder.Environment.IsEnvironment("IntegrationTests"))
{
    builder.Services.AddDbContext<RestaurantDbContext>(options =>
        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
}
else
{
    builder.Services.AddDbContext<RestaurantDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

//   
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

//  Swagger/OpenAPI    API
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Restaurant API",
        Version = "v1",
        Description = "API   "
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
    foreach (var xmlFile in xmlFiles)
    {
        c.IncludeXmlComments(xmlFile);
    }
});

//  JSON   
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
    });

//  JWT 
var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyString = jwtSettings["Key"];
if (string.IsNullOrEmpty(keyString))
{
    throw new InvalidOperationException("JWT Key is not configured in appsettings.json.");
}
var key = Encoding.ASCII.GetBytes(keyString);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key) { KeyId = "main_signing_key" },
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["access_token"];
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379"));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ����������� �������� ��� ������ � �������� � ���������������
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReportService, ReportService>();

var app = builder.Build();

// ��������� Swagger UI ��� ��������� � ������������ API
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant API v1");
    c.RoutePrefix = string.Empty;
    c.DisplayRequestDuration();
});

// �������� ���� ������ � ������������� �������������� �� ���������
if (!app.Environment.IsEnvironment("IntegrationTests"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        db.Database.Migrate();

        var adminRole = await db.RoleUsers.FirstOrDefaultAsync(r => r.Name == "Admin");

        if (adminRole == null)
        {
            Console.WriteLine("��������: ���� 'Admin' �� ������� � ���� ������. ���������� ������� �������������� �� ���������.");
        }
        else
        {
            var adminExists = await db.Users.AnyAsync(u => u.RoleId == adminRole.Id);

            if (!adminExists)
            {
                var defaultAdminEmail = builder.Configuration["DefaultAdmin:Email"];
                var defaultAdminPassword = builder.Configuration["DefaultAdmin:Password"];
                var defaultAdminPhone = builder.Configuration["DefaultAdmin:Phone"];
                var defaultAdminName = builder.Configuration["DefaultAdmin:Name"];

                if (string.IsNullOrEmpty(defaultAdminEmail) || string.IsNullOrEmpty(defaultAdminPassword))
                {
                    Console.WriteLine("��������: �� ������� ������� �������������� �� ���������. ��������� ������ 'DefaultAdmin:Email' � 'DefaultAdmin:Password' � ������������.");
                }
                else
                {
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(defaultAdminPassword);

                    var defaultAdminUser = new RestaurantBackend.Models.UserModel
                    {
                        Id = Guid.NewGuid(),
                        Email = defaultAdminEmail,
                        Phone = defaultAdminPhone ?? "00000000000",
                        Name = defaultAdminName ?? "Default Admin",
                        PasswordHash = hashedPassword,
                        RoleId = adminRole.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await userRepository.AddAsync(defaultAdminUser);
                    await userRepository.SaveChangesAsync();

                    Console.WriteLine($"������������� �� ��������� '{defaultAdminEmail}' ������� ������.");
                }
            }
            else
            {
                Console.WriteLine("������������� ��� ����������. ���������� �������� �������������� �� ���������.");
            }
        }
    }
}

// Custom middleware ��� ��������������� ���������� access ������
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (!path.StartsWithSegments("/api/auth") &&
        !path.StartsWithSegments("/swagger") &&
        context.Request.Cookies["access_token"] != null)
    {
        var token = context.Request.Cookies["access_token"];
        var jwtHandler = new JwtSecurityTokenHandler();

        if (jwtHandler.CanReadToken(token))
        {
            var jwtToken = jwtHandler.ReadJwtToken(token);
            var exp = jwtToken.ValidTo;

            if (exp < DateTime.UtcNow.AddMinutes(5))
            {
                var refreshToken = context.Request.Cookies["refresh_token"];
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var authService = context.RequestServices.GetRequiredService<IAuthService>();
                    try
                    {
                        var newTokens = await authService.RefreshToken(token, refreshToken);

                        context.Response.Cookies.Append("access_token", newTokens.Token,
                            new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.Strict,
                                Expires = newTokens.Expiration
                            });

                        context.Response.Cookies.Append("refresh_token", newTokens.RefreshToken,
                            new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.Strict,
                                Expires = DateTime.UtcNow.AddDays(
                                    Convert.ToDouble(context.RequestServices
                                        .GetRequiredService<IConfiguration>()
                                        ["Jwt:RefreshTokenExpirationDays"]))
                            });
                    }
                    catch (SecurityTokenException ex)
                    {
                        Console.WriteLine($"������ ���������� ������: {ex.Message}");
                        context.Response.Cookies.Delete("access_token");
                        context.Response.Cookies.Delete("refresh_token");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"�������������� ������ ��� ���������� ������: {ex.Message}");
                    }
                }
            }
        }
    }

    await next();
});

// ��������������� HTTP �������� �� HTTPS
app.UseHttpsRedirection();
// ��������� CORS ��������
app.UseCors("AllowAll");
// ��������� middleware ��������������
app.UseAuthentication();
// ��������� middleware �����������
app.UseAuthorization();
// ������� ������������
app.MapControllers();

app.Run();