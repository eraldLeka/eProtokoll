using eProtokoll.Data;
using eProtokoll.Repositories;
using eProtokoll.Repositories.AuditLogs;
using eProtokoll.Repositories.Dashboard;
using eProtokoll.Repositories.Documents;
using eProtokoll.Repositories.Institutions;
using eProtokoll.Repositories.User;
using eProtokoll.Services.Files;
using eProtokoll.Services.ProtocolNumber;
using eProtokoll.Services.Report;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ==================== AUTH ====================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;

        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

// ==================== MVC ====================
builder.Services.AddControllersWithViews();

// ==================== REPOSITORIES ====================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<ITrackingRepository, TrackingRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IInstitutionRepository, InstitutionRepository>();

// ==================== SERVICES ====================
builder.Services.AddScoped<IProtocolNumberService, ProtocolNumberService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IFileSecurityService, FileSecurityService>();
builder.Services.AddScoped<IDocumentFileService, DocumentFileService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ReportService>();

// ==================== CACHE & SESSION ====================
builder.Services.AddMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

// ==================== SEED ====================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var userRepo = services.GetRequiredService<IUserRepository>();
        await DbSeeder.SeedAdminUser(userRepo);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error while seeding admin user.");
    }
}

// ==================== MIDDLEWARE ====================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// ==================== ROUTING ====================
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();