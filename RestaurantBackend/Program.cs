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

// Настройка CORS политики для разрешения запросов со всех источников
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
    });
});

// Добавление DbContext для работы с базой данных PostgreSQL
builder.Services.AddDbContext<RestaurantDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация общих репозиториев и специфичного репозитория пользователей
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Настройка Swagger/OpenAPI для генерации документации API
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Restaurant API",
        Version = "v1",
        Description = "API для управления рестораном"
    });

    // Добавление схемы безопасности для JWT Bearer токенов в Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    // Добавление требования безопасности для всех эндпоинтов в Swagger UI, чтобы можно было авторизоваться
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

    // Включение XML-комментариев для документации Swagger (из файлов XML)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Настройка JSON сериализации для контроллеров
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Настройка JWT аутентификации
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
        ValidateIssuer = true, // Проверять издателя токена
        ValidateAudience = true, // Проверять аудиторию токена
        ValidateLifetime = true, // Проверять срок действия токена
        ValidateIssuerSigningKey = true, // Проверять подпись токена
        ValidIssuer = jwtSettings["Issuer"], // Указываем ожидаемого издателя
        ValidAudience = jwtSettings["Audience"], // Указываем ожидаемую аудиторию
        IssuerSigningKey = new SymmetricSecurityKey(key), // Указываем ключ для проверки подписи
        ClockSkew = TimeSpan.Zero // Отключение временного смещения для проверки срока действия
    };

    // Настройка обработки токенов, полученных из куки
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Извлекаем токен из куки с именем "access_token"
            context.Token = context.Request.Cookies["access_token"];
            return Task.CompletedTask;
        }
    };
});

// Регистрация сервисов для работы с токенами и аутентификацией
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Включение Swagger UI для просмотра и тестирования API
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant API v1");
    c.RoutePrefix = string.Empty;
    c.DisplayRequestDuration();
});

// Миграция базы данных и инициализация администратора по умолчанию
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

    db.Database.Migrate();

    var adminRole = await db.RoleUsers.FirstOrDefaultAsync(r => r.Name == "Admin");

    if (adminRole == null)
    {
        Console.WriteLine("ВНИМАНИЕ: Роль 'Admin' не найдена в базе данных. Невозможно создать администратора по умолчанию.");
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
                Console.WriteLine("ВНИМАНИЕ: Не удалось создать администратора по умолчанию. Проверьте секции 'DefaultAdmin:Email' и 'DefaultAdmin:Password' в конфигурации.");
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

                Console.WriteLine($"Администратор по умолчанию '{defaultAdminEmail}' успешно создан.");
            }
        }
        else
        {
            Console.WriteLine("Администратор уже существует. Пропускаем создание администратора по умолчанию.");
        }
    }
}

// Custom middleware для автоматического обновления access токена
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (!path.StartsWithSegments("/api/auth") &&
        !path.StartsWithSegments("/swagger") &&
        context.Request.Cookies["access_token"] != null)
    {
        var token = context.Request.Cookies["access_token"];
        var jwtHandler = new JwtSecurityTokenHandler();

        // Если токен можно прочитать
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
                       
                        Console.WriteLine($"Ошибка обновления токена: {ex.Message}");
                        context.Response.Cookies.Delete("access_token");
                        context.Response.Cookies.Delete("refresh_token");
                    }
                    catch (Exception ex)
                    {
                        
                        Console.WriteLine($"Непредвиденная ошибка при обновлении токена: {ex.Message}");
                    }
                }
            }
        }
    }

    await next(); // Передаем управление следующему middleware в цепочке
});

// Перенаправление HTTP запросов на HTTPS
app.UseHttpsRedirection();
// Включение CORS политики
app.UseCors("AllowAll");
// Включение middleware аутентификации (проверяет наличие и валидность токенов)
app.UseAuthentication();
// Включение middleware авторизации (проверяет права доступа на основе ролей/политик)
app.UseAuthorization();
// Маппинг контроллеров для обработки входящих HTTP запросов
app.MapControllers();

app.Run();
