using FinancialInsights.Api.Config;
using FinancialInsights.Api.Data;
using FinancialInsights.Api.Middleware;
using FinancialInsights.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.Services.Configure<AkahuOptions>(builder.Configuration.GetSection(AkahuOptions.SectionName));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:4173",
                "http://127.0.0.1:5173",
                "http://127.0.0.1:4173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Host=localhost;Port=5432;Database=financial_insights;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

builder.Services.AddHttpClient("Akahu");

builder.Services.AddScoped<IAkahuClient, AkahuClient>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IContactResolutionService, ContactResolutionService>();
builder.Services.AddScoped<ITransferMatchingService, TransferMatchingService>();
builder.Services.AddSingleton<INzBankCatalogService, NzBankCatalogService>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");

app.MapHealthChecks("/health");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
    else
    {
        dbContext.Database.EnsureCreated();
    }
}

app.Run();

public partial class Program;
