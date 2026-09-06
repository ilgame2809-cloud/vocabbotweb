using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VocabBotWeb.Api.Data;
using VocabBotWeb.Api.Models;
using VocabBotWeb.Api.Services;

// По умолчанию ASP.NET переименовывает входящий claim "sub" в
// ClaimTypes.NameIdentifier — отключаем, чтобы контроллеры могли читать
// JwtRegisteredClaimNames.Sub напрямую (см. WordsController.CurrentUserId).
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var builder = WebApplication.CreateBuilder(args);

// ---------- БД ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ---------- Identity ----------
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        // Разумные дефолты для обычного сайта, не телеграм-бота — паролю
        // не нужно быть 6-значным PIN-кодом, как было бы удобно в чате.
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false; // включим, когда подключим отправку писем
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ---------- JWT-аутентификация (React будет слать Authorization: Bearer <token>) ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(options =>
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
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!)),
        };
    });

builder.Services.AddControllers();

// ---------- Сервисы бизнес-логики (перенесены из Db.cs бота) ----------
builder.Services.AddScoped<WordSelectionService>();

// ---------- Swagger (удобно тыкать API руками, пока нет фронта) ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Кнопка "Authorize" в Swagger UI — чтобы можно было вставить Bearer-токен
    // и сразу проверять защищённые эндпоинты руками.
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ---------- CORS для React (Vite dev-сервер по умолчанию на :5173) ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("FrontendOrigins").Get<string[]>()
                            ?? new[] { "http://localhost:5173" })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// ---------- Автоприменение миграций при старте ----------
// На Render (особенно на бесплатном плане) нет удобного способа зайти
// в контейнер и руками прогнать `dotnet ef database update`, поэтому
// применяем миграции на старте приложения. Для локальной разработки это
// тоже ок — просто выполнится мгновенно, если БД уже актуальна.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // ---------- Сидинг словаря ----------
    // Перенос WordSeed.cs из бота — идемпотентно (см. WordSeedService),
    // так что при каждом следующем деплое просто ничего не делает, если
    // словарь уже загружен.
    await WordSeedService.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "VocabBotWeb API is running");
app.MapControllers();

// Дальше сюда будут добавляться следующие вертикальные срезы:
// decks (свои колоды), grammar (тренажёр), mock-tests, study-plans, progress...

app.Run();
