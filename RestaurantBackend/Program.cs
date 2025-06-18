using Microsoft.EntityFrameworkCore;
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

var builder = WebApplication.CreateBuilder(args);

// ��������� CORS �������� ��� ���������� �������� �� ���� ����������
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
    });
});

// ���������� DbContext ��� ������ � ����� ������ PostgreSQL
builder.Services.AddDbContext<RestaurantDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ����������� ����� ������������ � ������������ ����������� �������������
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();

// ��������� Swagger/OpenAPI ��� ��������� ������������ API
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Restaurant API",
        Version = "v1",
        Description = "API ��� ���������� ����������"
    });

    // ���������� ����� ������������ ��� JWT Bearer ������� � Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    // ���������� ���������� ������������ ��� ���� ���������� � Swagger UI, ����� ����� ���� ��������������
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

    // ��������� XML-������������ ��� ������������ Swagger (�� ������ XML)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// ��������� JSON ������������ ��� ������������
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// ��������� JWT ��������������
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, // ��������� �������� ������
        ValidateAudience = true, // ��������� ��������� ������
        ValidateLifetime = true, // ��������� ���� �������� ������
        ValidateIssuerSigningKey = true, // ��������� ������� ������
        ValidIssuer = jwtSettings["Issuer"], // ��������� ���������� ��������
        ValidAudience = jwtSettings["Audience"], // ��������� ��������� ���������
        IssuerSigningKey = new SymmetricSecurityKey(key), // ��������� ���� ��� �������� �������
        ClockSkew = TimeSpan.Zero // ���������� ���������� �������� ��� �������� ����� ��������
    };

    // ��������� ��������� �������, ���������� �� ����
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // ��������� ����� �� ���� � ������ "access_token"
            context.Token = context.Request.Cookies["access_token"];
            return Task.CompletedTask;
        }
    };
});

// ����������� �������� ��� ������ � �������� � ���������������
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

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

        // ���� ����� ����� ���������
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

    await next(); // �������� ���������� ���������� middleware � �������
});

// ��������������� HTTP �������� �� HTTPS
app.UseHttpsRedirection();
// ��������� CORS ��������
app.UseCors("AllowAll");
// ��������� middleware �������������� (��������� ������� � ���������� �������)
app.UseAuthentication();
// ��������� middleware ����������� (��������� ����� ������� �� ������ �����/�������)
app.UseAuthorization();
// ������� ������������ ��� ��������� �������� HTTP ��������
app.MapControllers();

app.Run();
