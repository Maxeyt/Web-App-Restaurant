using System.Text.Json;
using System.Text.Json.Serialization;
using MaxFood.Data;
using MaxFood.Models;
using MaxFood.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// ========== НАСТРОЙКА ЛОГИРОВАНИЯ ==========
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("MaxFood", LogLevel.Debug);
// ============================================

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MaxFoodDBContext>(options =>
    options.UseSqlServer(connectionString));

// ========== РЕГИСТРАЦИЯ СЕРВИСОВ ==========
builder.Services.AddScoped<NotificationService>();
// ==========================================

// ========== НАСТРОЙКА СЕССИИ ==========
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});
// =====================================

// ========== НАСТРОЙКА КОНТРОЛЛЕРОВ ==========
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
// ====================================================

// ========== НАСТРОЙКА RAZOR PAGES ==========
// ★★★ НАСТРОЙКА АНТИФОРЖЕРИ ДЛЯ БЕЗОПАСНОСТИ ★★★
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";  // Заголовок для AJAX запросов
    options.SuppressXFrameOptionsHeader = true;
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToPage("/Authorization/Login");
    options.Conventions.AllowAnonymousToPage("/Registration/Register");
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Menu/Index");
    options.Conventions.AllowAnonymousToPage("/Menu/Details");
});
// ====================================================

// ========== НАСТРОЙКА HANGFIRE ==========
builder.Services.AddHangfire(configuration => configuration
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer(options =>
{
    options.SchedulePollingInterval = TimeSpan.FromSeconds(1);
});
// ==========================================

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Приложение запущено в режиме: {Environment}", app.Environment.EnvironmentName);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

// ========== ДАШБОРД HANGFIRE ==========
app.UseHangfireDashboard("/hangfire");
// ==========================================

app.MapRazorPages();
app.MapControllers();

// ===== АВТОМАТИЧЕСКОЕ СНЯТИЕ NOT NULL С DishVariantId =====
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<MaxFoodDBContext>();
        dbContext.Database.ExecuteSqlRaw(
            "IF EXISTS (SELECT 1 FROM sys.columns c " +
            "JOIN sys.tables t ON c.object_id = t.object_id " +
            "WHERE t.name = 'reviews' AND c.name = 'DishVariantId' AND c.is_nullable = 0) " +
            "BEGIN " +
            "ALTER TABLE reviews ALTER COLUMN DishVariantId int NULL " +
            "END");
        logger.LogInformation("Столбец DishVariantId проверен/изменён на NULL");
    }
}
catch (Exception ex)
{
    logger.LogWarning(ex, "Не удалось изменить столбец DishVariantId");
}

try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<MaxFoodDBContext>();
        dbContext.Database.CanConnect();
        logger.LogInformation("Подключение к базе данных успешно");
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Ошибка подключения к базе данных");
}

app.Run();